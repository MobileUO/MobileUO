// SPDX-License-Identifier: BSD-2-Clause

using ClassicUO.Utility;
using System;

namespace ClassicUO.Network.Encryption
{
    internal enum EncryptionType
    {
        NONE,
        OLD_BFISH,
        BLOWFISH__1_25_36,
        BLOWFISH,
        BLOWFISH__2_0_3,
        TWOFISH_MD5
    }

    internal sealed class EncryptionHelper
    {
        private static readonly LoginCryptBehaviour _loginCrypt = new LoginCryptBehaviour();
        private static readonly BlowfishEncryption _blowfishEncryption = new BlowfishEncryption();
        private static readonly TwofishEncryption _twoFishBehaviour = new TwofishEncryption();


        public uint KEY_1, KEY_2, KEY_3;
        private readonly ClientVersion _clientVersion;
        public EncryptionType Type;
        public EncryptionType EncryptionType { get => Type; }

        public EncryptionHelper(ClientVersion clientVersion)
        {
            _clientVersion = clientVersion;
            /*(EncryptionType, _keys) = */CalculateEncryption(clientVersion);
        }


        public void CalculateEncryption(ClientVersion version)
        {
            if (version == ClientVersion.CV_200X)
            {
                KEY_1 = 0x2D13A5FC;
                KEY_2 = 0x2D13A5FD;
                KEY_3 = 0xA39D527F;

                Type = EncryptionType.BLOWFISH__2_0_3;
            }
            else
            {
                int a = ((int) version >> 24) & 0xFF;
                int b = ((int) version >> 16) & 0xFF;
                int c = ((int) version >> 8) & 0xFF;

                int temp = ((((a << 9) | b) << 10) | c) ^ ((c * c) << 5);

                KEY_2 = (uint) ((temp << 4) ^ (b * b) ^ (b * 0x0B000000) ^ (c * 0x380000) ^ 0x2C13A5FD);
                temp = (((((a << 9) | c) << 10) | b) * 8) ^ (c * c * 0x0c00);
                KEY_3 = (uint) (temp ^ (b * b) ^ (b * 0x6800000) ^ (c * 0x1c0000) ^ 0x0A31D527F);
                KEY_1 = KEY_2 - 1;


                if (version < (ClientVersion) (((1 & 0xFF) << 24) | ((25 & 0xFF) << 16) | ((35 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = EncryptionType.OLD_BFISH;
                }


                else if (version == (ClientVersion) (((1 & 0xFF) << 24) | ((25 & 0xFF) << 16) | ((36 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = EncryptionType.BLOWFISH__1_25_36;
                }


                else if (version <= ClientVersion.CV_200)
                {
                    Type = EncryptionType.BLOWFISH;
                }

                else if (version <= (ClientVersion) (((2 & 0xFF) << 24) | ((0 & 0xFF) << 16) | ((3 & 0xFF) << 8) | (0 & 0xFF)))
                {
                    Type = EncryptionType.BLOWFISH__2_0_3;
                }
                else
                {
                    Type = EncryptionType.TWOFISH_MD5;
                }
            }
        }


        public void Initialize(bool is_login, uint seed)
        {
            EncryptionType encryption = Type;

            if (encryption == EncryptionType.NONE)
            {
                return;
            }

            if (is_login)
            {
                _loginCrypt.Initialize(seed, KEY_1, KEY_2, KEY_3);
            }
            else
            {
                if (encryption >= EncryptionType.OLD_BFISH && encryption < EncryptionType.TWOFISH_MD5)
                {
                    _blowfishEncryption.Initialize();
                }

                if (encryption == EncryptionType.BLOWFISH__2_0_3 || encryption == EncryptionType.TWOFISH_MD5)
                {
                    _twoFishBehaviour.Initialize(seed, encryption == EncryptionType.TWOFISH_MD5);
                }
            }
        }


        public void Encrypt(bool is_login, Span<byte> src, Span<byte> dst, int size)
        {
            if (Type == EncryptionType.NONE)
            {
                return;
            }

            if (is_login)
            {
                if (Type == EncryptionType.OLD_BFISH)
                {
                    _loginCrypt.Encrypt_OLD(src, dst, size);
                }
                else if (Type == EncryptionType.BLOWFISH__1_25_36)
                {
                    _loginCrypt.Encrypt_1_25_36(src, dst, size);
                }
                else if (Type != EncryptionType.NONE)
                {
                    _loginCrypt.Encrypt(src, dst, size);
                }
            }
            else if (Type == EncryptionType.BLOWFISH__2_0_3)
            {
                int index_s = 0, index_d = 0;

                _blowfishEncryption.Encrypt
                (
                    src,
                    dst,
                    size,
                    ref index_s,
                    ref index_d
                );

                _twoFishBehaviour.Encrypt(dst, dst, size);
            }
            else if (Type == EncryptionType.TWOFISH_MD5)
            {
                _twoFishBehaviour.Encrypt(src, dst, size);
            }
            else
            {
                int index_s = 0, index_d = 0;

                _blowfishEncryption.Encrypt
                (
                    src,
                    dst,
                    size,
                    ref index_s,
                    ref index_d
                );
            }
        }

        public void Decrypt(Span<byte> src, Span<byte> dst, int size)
        {
            if (Type == EncryptionType.TWOFISH_MD5)
            {
                _twoFishBehaviour.Decrypt(src, dst, size);
            }
        }
    }
}

// MobileUO: TODO: collection expressions feature is not available in Unity's C#
//using ClassicUO.Utility;
//using System;

//namespace ClassicUO.Network.Encryption
//{
//    internal enum EncryptionType
//    {
//        NONE,
//        OLD_BFISH,
//        BLOWFISH__1_25_36,
//        BLOWFISH,
//        BLOWFISH__2_0_3,
//        TWOFISH_MD5
//    }

//    internal sealed class EncryptionHelper
//    {
//        private static readonly LoginCryptBehaviour _loginCrypt = new LoginCryptBehaviour();
//        private static readonly BlowfishEncryption _blowfishEncryption = new BlowfishEncryption();
//        private static readonly TwofishEncryption _twoFishBehaviour = new TwofishEncryption();


//        private readonly ClientVersion _clientVersion;
//        private readonly uint[] _keys;

//        public EncryptionHelper(ClientVersion clientVersion)
//        {
//            _clientVersion = clientVersion;
//            (EncryptionType, _keys) = CalculateEncryption(clientVersion);
//        }

//        public EncryptionType EncryptionType { get; }


//        private static (EncryptionType, uint[]) CalculateEncryption(ClientVersion version)
//        {
//            if (version == ClientVersion.CV_200X)
//            {
//                return (EncryptionType.BLOWFISH__2_0_3, [0x2D13A5FC, 0x2D13A5FD, 0xA39D527F]);
//            }

//            int a = ((int)version >> 24) & 0xFF;
//            int b = ((int)version >> 16) & 0xFF;
//            int c = ((int)version >> 8) & 0xFF;

//            int temp = ((((a << 9) | b) << 10) | c) ^ ((c * c) << 5);

//            var key2 = (uint)((temp << 4) ^ (b * b) ^ (b * 0x0B000000) ^ (c * 0x380000) ^ 0x2C13A5FD);
//            temp = (((((a << 9) | c) << 10) | b) * 8) ^ (c * c * 0x0c00);
//            var key3 = (uint)(temp ^ (b * b) ^ (b * 0x6800000) ^ (c * 0x1c0000) ^ 0x0A31D527F);
//            var key1 = key2 - 1;

//            switch (version)
//            {
//                case < (ClientVersion)((1 & 0xFF) << 24 | (25 & 0xFF) << 16 | (35 & 0xFF) << 8 | 0 & 0xFF):
//                    return (EncryptionType.OLD_BFISH, [key1, key2, key3]);
//                case (ClientVersion)((1 & 0xFF) << 24 | (25 & 0xFF) << 16 | (36 & 0xFF) << 8 | 0 & 0xFF):
//                    return (EncryptionType.BLOWFISH__1_25_36, [key1, key2, key3]);
//                case <= ClientVersion.CV_200:
//                    return (EncryptionType.BLOWFISH, [key1, key2, key3]);
//                case <= (ClientVersion)((2 & 0xFF) << 24 | (0 & 0xFF) << 16 | (3 & 0xFF) << 8 | 0 & 0xFF):
//                    return (EncryptionType.BLOWFISH__2_0_3, [key1, key2, key3]);
//                default:
//                    return (EncryptionType.TWOFISH_MD5, [key1, key2, key3]);
//            }
//        }


//        public void Initialize(bool isLogin, uint seed)
//        {
//            if (EncryptionType == EncryptionType.NONE)
//            {
//                return;
//            }

//            if (isLogin)
//            {
//                _loginCrypt.Initialize(seed, _keys[0], _keys[1], _keys[2]);
//            }
//            else
//            {
//                if (EncryptionType >= EncryptionType.OLD_BFISH && EncryptionType < EncryptionType.TWOFISH_MD5)
//                {
//                    _blowfishEncryption.Initialize();
//                }

//                if (EncryptionType == EncryptionType.BLOWFISH__2_0_3 || EncryptionType == EncryptionType.TWOFISH_MD5)
//                {
//                    _twoFishBehaviour.Initialize(seed, EncryptionType == EncryptionType.TWOFISH_MD5);
//                }
//            }
//        }

//        public void Encrypt(bool is_login, Span<byte> src, Span<byte> dst, int size)
//        {
//            if (EncryptionType == EncryptionType.NONE)
//            {
//                return;
//            }

//            if (is_login)
//            {
//                if (EncryptionType == EncryptionType.OLD_BFISH)
//                {
//                    _loginCrypt.Encrypt_OLD(src, dst, size);
//                }
//                else if (EncryptionType == EncryptionType.BLOWFISH__1_25_36)
//                {
//                    _loginCrypt.Encrypt_1_25_36(src, dst, size);
//                }
//                else if (EncryptionType != EncryptionType.NONE)
//                {
//                    _loginCrypt.Encrypt(src, dst, size);
//                }
//            }
//            else if (EncryptionType == EncryptionType.BLOWFISH__2_0_3)
//            {
//                int index_s = 0, index_d = 0;

//                _blowfishEncryption.Encrypt
//                (
//                    src,
//                    dst,
//                    size,
//                    ref index_s,
//                    ref index_d
//                );

//                _twoFishBehaviour.Encrypt(dst, dst, size);
//            }
//            else if (EncryptionType == EncryptionType.TWOFISH_MD5)
//            {
//                _twoFishBehaviour.Encrypt(src, dst, size);
//            }
//            else
//            {
//                int index_s = 0, index_d = 0;

//                _blowfishEncryption.Encrypt
//                (
//                    src,
//                    dst,
//                    size,
//                    ref index_s,
//                    ref index_d
//                );
//            }
//        }

//        public void Decrypt(Span<byte> src, Span<byte> dst, int size)
//        {
//            if (EncryptionType == EncryptionType.TWOFISH_MD5)
//            {
//                _twoFishBehaviour.Decrypt(src, dst, size);
//            }
//        }
//    }
//}}