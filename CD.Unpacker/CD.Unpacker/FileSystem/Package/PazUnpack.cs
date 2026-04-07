using System;
using System.Collections.Generic;
using System.IO;

namespace CD.Unpacker
{
    class PazUnpack
    {
        private static List<MetaFolder> m_FoldersTable = new List<MetaFolder>();
        private static List<MetaEntry> m_FilesTable = new List<MetaEntry>();

        public static void iDoIt(String m_MetaFile, String m_DstFolder)
        {
            using (FileStream TMetaStream = File.OpenRead(m_MetaFile))
            {
                var m_Header = new MetaHeader();

                m_Header.dwChecksum = TMetaStream.ReadUInt32();
                m_Header.dwTotalArchives = TMetaStream.ReadInt32();
                m_Header.dwMagic = TMetaStream.ReadUInt32();

                if (m_Header.dwMagic != 0x610E0232 && m_Header.dwMagic != 0)
                {
                    throw new Exception("[ERROR]: Invalid magic of META file!");
                }

                for (Int32 i = 0; i < m_Header.dwTotalArchives; i++)
                {
                    Int32 dwPazId = TMetaStream.ReadInt32();
                    UInt32 dwPazCheckSum = TMetaStream.ReadUInt32();
                    Int32 dwPazSize = TMetaStream.ReadInt32();
                }

                MetaStrings.iReadStringTables(TMetaStream);

                m_Header.dwTotalFolders = TMetaStream.ReadInt32();

                m_FoldersTable.Clear();
                for (Int32 i = 0; i < m_Header.dwTotalFolders; i++)
                {
                    var m_Folder = new MetaFolder();

                    m_Folder.dwChecksum = TMetaStream.ReadUInt32();
                    m_Folder.dwFolderNameOffset = TMetaStream.ReadInt32();
                    m_Folder.dwUnknown1 = TMetaStream.ReadInt32();
                    m_Folder.dwTotalFiles = TMetaStream.ReadInt32();

                    m_FoldersTable.Add(m_Folder);
                }

                m_Header.dwTotalFiles = TMetaStream.ReadInt32();

                m_FilesTable.Clear();
                for (Int32 i = 0; i < m_Header.dwTotalFiles; i++)
                {
                    var m_Entry = new MetaEntry();

                    m_Entry.dwFileNameOffset = TMetaStream.ReadInt32();
                    m_Entry.dwOffset = TMetaStream.ReadUInt32();
                    m_Entry.dwCompressedSize = TMetaStream.ReadInt32();
                    m_Entry.dwDecompressedSize = TMetaStream.ReadInt32();
                    m_Entry.wArchiveId = TMetaStream.ReadInt16();
                    m_Entry.wFlags = TMetaStream.ReadInt16();
                    m_Entry.wEncryption = (Encryption)(m_Entry.wFlags >> 4);
                    m_Entry.wCompression = (Compression)(m_Entry.wFlags & 0xF);

                    m_FilesTable.Add(m_Entry);
                }

                Int32 j = 0;
                foreach (var m_Folder in m_FoldersTable)
                {
                    String m_FullPath = null;
                    String m_FileName = null;
                    String m_FolderName = null;

                    if (m_Folder.dwFolderNameOffset != -1)
                    {
                        m_FolderName = MetaStrings.iGetFolderNameByOffset(m_Folder.dwFolderNameOffset);
                    }
                    else
                    {
                        m_FolderName = "";
                    }

                    for (Int32 i = 0; i < m_Folder.dwTotalFiles; i++, j++)
                    {
                        String m_ArchiveName = Path.GetDirectoryName(m_MetaFile) + @"\" + m_FilesTable[j].wArchiveId.ToString() + ".paz";

                        m_FileName = MetaStrings.iGetFileNameByOffset(m_FilesTable[j].dwFileNameOffset);
                        m_FullPath = m_DstFolder + m_FolderName.Replace("/", @"\") + m_FileName;

                        Utils.iSetInfo("[UNPACKING]: " + m_FolderName + m_FileName);
                        Utils.iCreateDirectory(m_FullPath);

                        if (!File.Exists(m_ArchiveName))
                        {
                            Utils.iSetError("[ERROR]: Unable to open PAZ file -> " + m_ArchiveName + " <- does not exist");
                            return;
                        }

                        using (FileStream TPazStream = File.OpenRead(m_ArchiveName))
                        {
                            TPazStream.Seek(m_FilesTable[j].dwOffset, SeekOrigin.Begin);

                            var lpSrcBuffer = TPazStream.ReadBytes(m_FilesTable[j].dwCompressedSize);
                            var lpDstBuffer = new Byte[] { };

                            if (m_FilesTable[j].wEncryption != 0)
                            {
                                switch (m_FilesTable[j].wEncryption)
                                {
                                    case Encryption.ICE: lpSrcBuffer = ICE.iDecryptData(lpSrcBuffer, lpSrcBuffer.Length); break;
                                    case Encryption.AES: lpSrcBuffer = AES.iDecryptData(lpSrcBuffer, lpSrcBuffer.Length); break;
                                    case Encryption.CHACHA20: lpSrcBuffer = CHACHA20.iDecryptData(lpSrcBuffer, m_FileName); break;
                                }
                            }

                            if (m_FilesTable[j].dwDecompressedSize != m_FilesTable[j].dwCompressedSize)
                            {
                                switch (m_FilesTable[j].wCompression)
                                {
                                    case Compression.PARTIAL: break;
                                    case Compression.LZ4: lpDstBuffer = LZ4.iDecompress(lpSrcBuffer, m_FilesTable[j].dwDecompressedSize); break;
                                    case Compression.ZLIB: lpDstBuffer = ZLIB.iDecompress(lpSrcBuffer, m_FilesTable[j].dwDecompressedSize); break;
                                    case Compression.QUICKLZ: lpDstBuffer = QUICKLZ.iDecompress(lpSrcBuffer); break;
                                }

                                File.WriteAllBytes(m_FullPath, lpDstBuffer);
                            }
                            else
                            {
                                File.WriteAllBytes(m_FullPath, lpSrcBuffer);
                            }

                            TPazStream.Dispose();
                        }
                    }
                }

                TMetaStream.Dispose();
            }
        }
    }
}
