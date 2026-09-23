using System;
using System.IO;
using System.Text;

namespace CD.Unpacker
{
    class CHACHA20
    {
        private static readonly Int32 dwRounds = 20;
        private static readonly Int32 dwBlockSize = 64;
        private static UInt32[] m_State;
        private static Byte[] m_Sigma = Encoding.ASCII.GetBytes("expand 32-byte k");

        private static readonly UInt32[] m_XorKeys = {
            0x00000000, 0x0A0A0A0A, 0x0C0C0C0C, 0x06060606,
            0x0E0E0E0E, 0x0A0A0A0A, 0x06060606, 0x02020202,
        };

        private static UInt32[] m_Key = new UInt32[8]; // 32 bytes in total
        private static UInt32[] m_Nonce = new UInt32[4]; // 16 bytes in total

        private static UInt32 ROL(UInt32 dwValue, Int32 dwShift)
        {
            return (dwValue << dwShift) | (dwValue >> (dwBlockSize / 2 - dwShift));
        }

        private static UInt32 ROR(UInt32 dwValue, Int32 dwShift)
        {
            return (dwValue >> dwShift) | (dwValue << (dwBlockSize / 2 - dwShift));
        }

        private static UInt32 ADD(UInt32 dwValueA, UInt32 dwValueB)
        {
            return dwValueA + dwValueB;
        }

        private static UInt32 XOR(UInt32 dwValueA, UInt32 dwValueB)
        {
            return dwValueA ^ dwValueB;
        }

        public static void iInitState()
        {
            m_State = new UInt32[16];

            m_State[0] = BitConverter.ToUInt32(m_Sigma, 0);
            m_State[1] = BitConverter.ToUInt32(m_Sigma, 4);
            m_State[2] = BitConverter.ToUInt32(m_Sigma, 8);
            m_State[3] = BitConverter.ToUInt32(m_Sigma, 12);

            m_State[4] = m_Key[0];
            m_State[5] = m_Key[1];
            m_State[6] = m_Key[2];
            m_State[7] = m_Key[3];
            m_State[8] = m_Key[4];
            m_State[9] = m_Key[5];
            m_State[10] = m_Key[6];
            m_State[11] = m_Key[7];

            m_State[12] = m_Nonce[0];
            m_State[13] = m_Nonce[1];
            m_State[14] = m_Nonce[2];
            m_State[15] = m_Nonce[3];
        }

        private static void iQuarterRound(UInt32[] m_BlockState, Int32 a, Int32 b, Int32 c, Int32 d)
        {
            m_BlockState[a] = ADD(m_BlockState[a], m_BlockState[b]);
            m_BlockState[d] = ROL(XOR(m_BlockState[d], m_BlockState[a]), 16);

            m_BlockState[c] = ADD(m_BlockState[c], m_BlockState[d]);
            m_BlockState[b] = ROL(XOR(m_BlockState[b], m_BlockState[c]), 12);

            m_BlockState[a] = ADD(m_BlockState[a], m_BlockState[b]);
            m_BlockState[d] = ROL(XOR(m_BlockState[d], m_BlockState[a]), 8);

            m_BlockState[c] = ADD(m_BlockState[c], m_BlockState[d]);
            m_BlockState[b] = ROL(XOR(m_BlockState[b], m_BlockState[c]), 7);
        }

        private static Byte[] iDecryptBlocks(Byte[] lpBuffer, Int32 dwSize)
        {
            Int32 dwOffset = 0;
            Int32 dwRemainedSize = 0;

            Byte[] lpBlockData = new Byte[dwBlockSize];

            iInitState();

            while (dwOffset < dwSize)
            {
                iBlockTransform(lpBlockData, m_State);

                dwRemainedSize = dwBlockSize > dwSize - dwOffset ? dwSize - dwOffset : dwBlockSize;

                Byte[] lpTemp = new Byte[dwBlockSize];
                Array.Copy(lpBuffer, dwOffset, lpTemp, 0, dwRemainedSize);

                for (Int32 i = 0; i < dwRemainedSize; i++)
                {
                    lpBuffer[dwOffset + i] = (Byte)(lpBuffer[dwOffset + i] ^ lpBlockData[i]);
                }

                dwOffset += dwBlockSize;

                m_State[12]++;
            }

            return lpBuffer;
        }

        private static void iBlockTransform(Byte[] lpBlockData, UInt32[] m_State)
        {
            UInt32[] m_BlockState = (UInt32[])m_State.Clone();

            for (Int32 i = 0; i < dwRounds / 2; i++)
            {
                iQuarterRound(m_BlockState, 0, 4, 8, 12);
                iQuarterRound(m_BlockState, 1, 5, 9, 13);
                iQuarterRound(m_BlockState, 2, 6, 10, 14);
                iQuarterRound(m_BlockState, 3, 7, 11, 15);
                iQuarterRound(m_BlockState, 0, 5, 10, 15);
                iQuarterRound(m_BlockState, 1, 6, 11, 12);
                iQuarterRound(m_BlockState, 2, 7, 8, 13);
                iQuarterRound(m_BlockState, 3, 4, 9, 14);
            }

            Int32 dwOffset = 0;
            for (Int32 i = 0; i < 16; i++, dwOffset += 4)
            {
                UInt32 dwData = ADD(m_State[i], m_BlockState[i]);

                lpBlockData[dwOffset + 0] = (Byte)dwData;
                lpBlockData[dwOffset + 1] = (Byte)(dwData >> 8);
                lpBlockData[dwOffset + 2] = (Byte)(dwData >> 16);
                lpBlockData[dwOffset + 3] = (Byte)(dwData >> 24);
            }
        }

        private static void iGenerateKey(UInt32 dwHash)
        {
            dwHash ^= 0x60616263;

            for (Int32 i = 0; i < m_XorKeys.Length; i++)
            {
                m_Key[i] = dwHash ^ m_XorKeys[i];
            }
        }

        private static void iGenerateNonce(UInt32 dwHash)
        {
            for (Int32 i = 0; i < m_Nonce.Length; i++)
            {
                m_Nonce[i] = dwHash;
            }
        }

        public static Byte[] iDecryptData(Byte[] lpBuffer, String m_FileName)
        {
            UInt32 dwHash = Lookup3.iGetStringHash32(Path.GetFileName(m_FileName), Encoding.UTF8);

            iGenerateKey(dwHash);
            iGenerateNonce(dwHash);

            lpBuffer = iDecryptBlocks(lpBuffer, lpBuffer.Length);

            return lpBuffer;
        }
    }
}
