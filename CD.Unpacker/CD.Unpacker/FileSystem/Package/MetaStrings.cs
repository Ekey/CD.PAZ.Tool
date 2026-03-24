using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CD.Unpacker
{
    class MetaStrings
    {
        private static MemoryStream TFoldersStream;
        private static MemoryStream TFilesStream;

        public static void iReadStringTables(FileStream TMetaStream)
        {
            Int32 dwFoldersTableSize = TMetaStream.ReadInt32();

            if (dwFoldersTableSize > 0)
            {
                var m_FoldersList = TMetaStream.ReadBytes(dwFoldersTableSize);
                TFoldersStream = new MemoryStream(m_FoldersList);
            }

            Int32 dwNamesTableSize = TMetaStream.ReadInt32();

            if (dwNamesTableSize > 0)
            {
                var m_FileNamesList = TMetaStream.ReadBytes(dwNamesTableSize);
                TFilesStream = new MemoryStream(m_FileNamesList);
            }
        }

        public static String iGetFolderNameByOffset(Int32 dwOffset)
        {
            List<String> m_TempList = new List<String>();

            TFoldersStream.Seek(dwOffset, SeekOrigin.Begin);

            Int32 dwNextOffset = 0;

            do
            {
                dwNextOffset = TFoldersStream.ReadInt32();
                Int32 bStringLength = TFoldersStream.ReadByte();
                var m_String = Encoding.ASCII.GetString(TFoldersStream.ReadBytes(bStringLength));

                m_TempList.Add(m_String);

                if (dwNextOffset != -1)
                {
                    TFoldersStream.Seek(dwNextOffset, SeekOrigin.Begin);
                }

            }
            while (dwNextOffset != -1);

            String m_FolderName = null;

            for (Int32 i = m_TempList.Count - 1; i >= 0; i--)
            {
                m_FolderName += m_TempList[i].ToString();
            }

            return m_FolderName += "/";
        }

        public static String iGetFileNameByOffset(Int32 dwOffset)
        {
            List<String> m_TempList = new List<String>();

            TFilesStream.Seek(dwOffset, SeekOrigin.Begin);

            Int32 dwNextOffset = 0;

            do
            {
                dwNextOffset = TFilesStream.ReadInt32();
                Int32 bStringLength = TFilesStream.ReadByte();
                var m_String = Encoding.ASCII.GetString(TFilesStream.ReadBytes(bStringLength));

                m_TempList.Add(m_String);

                if (dwNextOffset != -1)
                {
                    TFilesStream.Seek(dwNextOffset, SeekOrigin.Begin);
                }

            }
            while (dwNextOffset != -1);

            String m_FileName = null;

            for (Int32 i = m_TempList.Count - 1; i >= 0; i--)
            {
                m_FileName += m_TempList[i].ToString();
            }

            return m_FileName;
        }
    }
}
