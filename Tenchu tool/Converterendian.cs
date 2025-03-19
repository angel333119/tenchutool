namespace Tenchu_tool
{
    internal class Converterendian
    {
        public static uint bigendian16(uint valor)
        {
            //Converte um valor de 16Bits de Big endian para Little endian

            uint be = (uint)((byte)(valor >> 8) | ((byte)valor << 8));
            return be;
        }

        public static uint bigendian32(uint valor)
        {
            //Converte um vlaor de 32Bits de Big endian para Little endian

            uint primeiroByte = valor >> 24;
            uint segundoByte = valor >> 8 & 0x00FF00;
            uint terceiroByte = valor << 24;
            uint quartoByte = valor << 8 & 0x00FF0000;
            return primeiroByte | segundoByte | terceiroByte | quartoByte;
        }

        ulong bigendian64(ulong valor)
        {
            //Converte um valor de 64Bits de Big endian para Little endian

            ulong primeiroByte = (valor >> 0) & 0xFF;
            ulong segundoByte = (valor >> 8) & 0xFF;
            ulong terceiroByte = (valor >> 16) & 0xFF;
            ulong quartoByte = (valor >> 24) & 0xFF;
            ulong quintoByte = (valor >> 32) & 0xFF;
            ulong sextoByte = (valor >> 40) & 0xFF;
            ulong setimoByte = (valor >> 48) & 0xFF;
            ulong oitavoByte = (valor >> 56) & 0xFF;
            return oitavoByte | setimoByte | sextoByte | quintoByte | quartoByte | terceiroByte | segundoByte | primeiroByte;
        }
    }
}