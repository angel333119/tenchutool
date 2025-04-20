namespace Tenchu_tool
{
    internal class Pad
    {
        public static int pad16(int tamanho)
        {
            int resultado = tamanho / 0x10;

            int resultado2 = tamanho % 0x10;

            int bloco = resultado;

            if (resultado2 != 0)
            {
                bloco = resultado + 1;
            }

            int tamanhocompadding = bloco * 0x10;

            return tamanhocompadding;
        }

        public static int pad800(int tamanho)
        {
            int resultado = tamanho / 0x800;

            int resultado2 = tamanho % 0x800;

            int bloco = resultado;

            if (resultado2 != 0)
            {
                bloco = resultado + 1;
            }

            int tamanhocompadding = bloco * 0x800;

            return tamanhocompadding;
        }

        public static long pad800long(long tamanho)
        {
            long resultado = tamanho / 0x800;

            long resultado2 = tamanho % 0x800;

            long bloco = resultado;

            if (resultado2 != 0)
            {
                bloco = resultado + 1;
            }

            long tamanhocompadding = bloco * 0x800;

            return tamanhocompadding;
        }

    }
}
