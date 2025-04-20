namespace Tenchu_tool
{
    class tabela
    {
        public static string Converterascii(int comparador)
        {
            string convertido;
            //return convertido; tá la no final.

            if (comparador >= 0x30 && comparador <= 0x39) //Começa a conversão dos caracteres
            {
                convertido = ((char)('0' + (comparador - 0x30))).ToString(); //Convertendo dentro do intervalo de 0 e 9.
            }
            else if (comparador >= 0x41 && comparador <= 0x5A)
            {
                convertido = ((char)('A' + (comparador - 0x41))).ToString(); //Convertendo dentro do intervalo de A e Z.
            }
            else if (comparador >= 0x61 && comparador <= 0x7A)
            {
                convertido = ((char)('a' + (comparador - 0x61))).ToString(); //Convertendo dentro do intervalo de a e z.
            }
            else if (comparador == 0x20)
            {
                convertido = ' '.ToString();
            }
            else if (comparador == 0x21)
            {
                convertido = ("!");
            }
            else if (comparador == 0x22)
            {
                convertido = '"'.ToString();
            }
            else if (comparador == 0x23)
            {
                convertido = ("#");
            }
            else if (comparador == 0x24)
            {
                convertido = ("$");
            }
            else if (comparador == 0x25)
            {
                convertido = ("%");
            }
            else if (comparador == 0x26)
            {
                convertido = ("&");
            }
            else if (comparador == 0x27)
            {
                convertido = ("'");
            }
            else if (comparador == 0x28)
            {
                convertido = ("(");
            }
            else if (comparador == 0x29)
            {
                convertido = (")");
            }
            else if (comparador == 0x2A)
            {
                convertido = ("*");
            }
            else if (comparador == 0x2B)
            {
                convertido = ("+");
            }
            else if (comparador == 0x2C)
            {
                convertido = (",");
            }
            else if (comparador == 0x2D)
            {
                convertido = ("-");
            }
            else if (comparador == 0x2E)
            {
                convertido = (".");
            }
            else if (comparador == 0x2F)
            {
                convertido = ("/");
            }
            else if (comparador == 0x3A)
            {
                convertido = (":");
            }
            else if (comparador == 0x3B)
            {
                convertido = (";");
            }
            else if (comparador == 0x3C)
            {
                convertido = ("<");
            }
            else if (comparador == 0x3D)
            {
                convertido = ("=");
            }
            else if (comparador == 0x3E)
            {
                convertido = (">");
            }
            else if (comparador == 0x3F)
            {
                convertido = ("?");
            }
            else if (comparador == 0x40)
            {
                convertido = ("@");
            }
            else if (comparador == 0xBF)
            {
                convertido = ("¿");
            }
            else if (comparador == 0xC0)
            {
                convertido = ("À");
            }
            else if (comparador == 0xC1)
            {
                convertido = ("Á");
            }
            else if (comparador == 0xC2)
            {
                convertido = ("Â");
            }
            else if (comparador == 0xC3)
            {
                convertido = ("Ã");
            }
            else if (comparador == 0xC4)
            {
                convertido = ("Ä");
            }
            else if (comparador == 0xC7)
            {
                convertido = ("Ç");
            }
            else if (comparador == 0xC8)
            {
                convertido = ("È");
            }
            else if (comparador == 0xC9)
            {
                convertido = ("É");
            }
            else if (comparador == 0xCA)
            {
                convertido = ("Ê");
            }
            else if (comparador == 0xCB)
            {
                convertido = ("Ë");
            }
            else if (comparador == 0xCC)
            {
                convertido = ("Ì");
            }
            else if (comparador == 0xCD)
            {
                convertido = ("Í");
            }
            else if (comparador == 0xCE)
            {
                convertido = ("Î");
            }
            else if (comparador == 0xCF)
            {
                convertido = ("Ï");
            }
            else if (comparador == 0xD1)
            {
                convertido = ("Ñ");
            }
            else if (comparador == 0xD2)
            {
                convertido = ("Ò");
            }
            else if (comparador == 0xD3)
            {
                convertido = ("Ó");
            }
            else if (comparador == 0xD4)
            {
                convertido = ("Ô");
            }
            else if (comparador == 0xD5)
            {
                convertido = ("Õ");
            }
            else if (comparador == 0xD6)
            {
                convertido = ("Ö");
            }
            else if (comparador == 0xD9)
            {
                convertido = ("Ù");
            }
            else if (comparador == 0xDA)
            {
                convertido = ("Ú");
            }
            else if (comparador == 0xDB)
            {
                convertido = ("Û");
            }
            else if (comparador == 0xDC)
            {
                convertido = ("Ü");
            }
            else if (comparador == 0xE0)
            {
                convertido = ("à");
            }
            else if (comparador == 0xE1)
            {
                convertido = ("á");
            }
            else if (comparador == 0xE2)
            {
                convertido = ("â");
            }
            else if (comparador == 0xE3)
            {
                convertido = ("ã");
            }
            else if (comparador == 0xE4)
            {
                convertido = ("ä");
            }
            else if (comparador == 0xE7)
            {
                convertido = ("ç");
            }
            else if (comparador == 0xE8)
            {
                convertido = ("è");
            }
            else if (comparador == 0xE9)
            {
                convertido = ("é");
            }
            else if (comparador == 0xEA)
            {
                convertido = ("ê");
            }
            else if (comparador == 0xEB)
            {
                convertido = ("ë");
            }
            else if (comparador == 0xEC)
            {
                convertido = ("ì");
            }
            else if (comparador == 0xED)
            {
                convertido = ("í");
            }
            else if (comparador == 0xEE)
            {
                convertido = ("î");
            }
            else if (comparador == 0xEF)
            {
                convertido = ("ï");
            }
            else if (comparador == 0xF1)
            {
                convertido = ("ñ");
            }
            else if (comparador == 0xF2)
            {
                convertido = ("ò");
            }
            else if (comparador == 0xF3)
            {
                convertido = ("ó");
            }
            else if (comparador == 0xF4)
            {
                convertido = ("ô");
            }
            else if (comparador == 0xF5)
            {
                convertido = ("õ");
            }
            else if (comparador == 0xF6)
            {
                convertido = ("ö");
            }
            else if (comparador == 0xF9)
            {
                convertido = ("ù");
            }
            else if (comparador == 0xFA)
            {
                convertido = ("ú");
            }
            else if (comparador == 0xFB)
            {
                convertido = ("û");
            }
            else if (comparador == 0xFC)
            {
                convertido = ("ü");
            }

            else
            {
                //Os valores que não estiverem na tabela, serão colocados em hex entre <>
                convertido = ("<" + comparador.ToString("X2") + ">");
                //convertido = comparador.ToString("X4");       mostra o valor em hex
                //comparador.ToString("<" + comparador + ">");  colocar o valor entre <>
            }
            return convertido;
        }


        public static int hextoascii(char caractere)
        {
            int convertido = 0;

            if (caractere >= '0' && caractere <= '9')
            {
                //Convertendo dentro do intervalo de a e z.
                convertido = (0x30 + (caractere - '0'));
            }
            else if (caractere >= 'A' && caractere <= 'Z')
            {
                //Convertendo dentro do intervalo de A e Z.
                convertido = (0x41 + (caractere - 'A'));
            }
            else if (caractere >= 'a' && caractere <= 'z')
            {
                //Convertendo dentro do intervalo de a e z.
                convertido = (0x61 + (caractere - 'a'));
            }
            else if (caractere == ' ')
            {
                convertido = 0x20;
            }
            else if (caractere == '!')
            {
                convertido = 0x21;
            }
            else if (caractere == '"')
            {
                convertido = 0x22;
            }
            else if (caractere == '$')
            {
                convertido = 0x24;
            }
            else if (caractere == '&')
            {
                convertido = 0x26;
            }
            else if (caractere == '\'')
            {
                convertido = 0x27;
            }
            else if (caractere == '(')
            {
                convertido = 0x28;
            }
            else if (caractere == ')')
            {
                convertido = 0x29;
            }
            else if (caractere == '+')
            {
                convertido = 0x2B;
            }
            else if (caractere == ',')
            {
                convertido = 0x2C;
            }
            else if (caractere == '-')
            {
                convertido = 0x2D;
            }
            else if (caractere == '.')
            {
                convertido = 0x2E;
            }
            else if (caractere == '/')
            {
                convertido = 0x2F;
            }
            else if (caractere == ':')
            {
                convertido = 0x3A;
            }
            else if (caractere == ';')
            {
                convertido = 0x3B;
            }
            else if (caractere == '?')
            {
                convertido = 0x3F;
            }
            else if (caractere == 'À')
            {
                convertido = 0xC0;
            }
            else if (caractere == 'Á')
            {
                convertido = 0xC1;
            }
            else if (caractere == 'Â')
            {
                convertido = 0xC2;
            }
            else if (caractere == 'Ã')
            {
                convertido = 0xC3;
            }
            else if (caractere == 'Ä')
            {
                convertido = 0xC4;
            }
            else if (caractere == 'Ç')
            {
                convertido = 0xC7;
            }
            else if (caractere == 'É')
            {
                convertido = 0xC9;
            }
            else if (caractere == 'Ê')
            {
                convertido = 0xCA;
            }
            else if (caractere == 'Í')
            {
                convertido = 0xCD;
            }
            else if (caractere == 'Ó')
            {
                convertido = 0xD3;
            }
            else if (caractere == 'Ô')
            {
                convertido = 0xD4;
            }
            else if (caractere == 'Õ')
            {
                convertido = 0xD5;
            }
            else if (caractere == 'Ö')
            {
                convertido = 0xD6;
            }
            else if (caractere == 'Ú')
            {
                convertido = 0xDA;
            }
            else if (caractere == 'à')
            {
                convertido = 0xE0;
            }
            else if (caractere == 'á')
            {
                convertido = 0xE1;
            }
            else if (caractere == 'â')
            {
                convertido = 0xE2;
            }
            else if (caractere == 'ã')
            {
                convertido = 0xE3;
            }
            else if (caractere == 'ä')
            {
                convertido = 0xE4;
            }
            else if (caractere == 'ç')
            {
                convertido = 0xE7;
            }
            else if (caractere == 'é')
            {
                convertido = 0xE9;
            }
            else if (caractere == 'ê')
            {
                convertido = 0xEA;
            }
            else if (caractere == 'í')
            {
                convertido = 0xED;
            }
            else if (caractere == 'ó')
            {
                convertido = 0xF3;
            }
            else if (caractere == 'ô')
            {
                convertido = 0xF4;
            }
            else if (caractere == 'õ')
            {
                convertido = 0xF5;
            }
            else if (caractere == 'ö')
            {
                convertido = 0xF6;
            }
            else if (caractere == 'ú')
            {
                convertido = 0xFA;
            }

            return convertido;
        }
    }
}
