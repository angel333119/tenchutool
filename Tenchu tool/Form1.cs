using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;



namespace Tenchu_tool
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        #region Tradução dos botões

        private Dictionary<string, string> _translations = new Dictionary<string, string>()
        {
            { "button1", "Extrair arquivo VOL" },
            { "button2", "Recriar arquivo VOL" },
            { "button3", "Extrair Texto" },
            { "button4", "Inserir Texto" },
            { "button5", "Extrair DAT" },
            { "button6", "Reconstruir DAT" },
            { "button7", "Extrair Texto" },
            { "button8", "Inserir Texto" },
            { "button9", "Extrair Texto" },
            { "button10", "Inserir Texto" },
            { "button11", "Extrair arquivo FARC" },
            { "button12", "Extrair Texto" },
            { "button13", "Inserir Texto" },
            { "button14", "Change language to english" },
            { "button17", "Extrair texturas" },
            { "button18", "Extrair rapidamente" },
            { "checkBox1", "Extrair texto Japonês" }

        };

        private Dictionary<string, string> _translationsEN = new Dictionary<string, string>()
        {
            { "button1", "Extract VOL file" },
            { "button2", "Recreate VOL file" },
            { "button3", "Extract Text" },
            { "button4", "Insert Text" },
            { "button5", "Extract DAT" },
            { "button6", "Reconstruct DAT" },
            { "button7", "Extract Text" },
            { "button8", "Insert Text" },
            { "button9", "Extract Text" },
            { "button10", "Insert Text" },
            { "button11", "Extract FARC file" },
            { "button12", "Extract Text" },
            { "button13", "Insert Text" },
            { "button14", "Mudar idioma para português" },
            { "button17", "Extract textures" },
            { "button18", "Extract textures quickly" },
            { "checkBox1", "Extract japanese text" }
        };

        private bool _isPortuguese = true;

        private void Form1_Load(object sender, EventArgs e)
        {
            UpdateButtonTranslations();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            _isPortuguese = !_isPortuguese;
            UpdateButtonTranslations();
            UpdateButton14Translation();
        }

        private void UpdateButtonTranslations()
        {
            var translations = _isPortuguese ? _translations : _translationsEN;

            foreach (var kvp in translations)
            {
                var control = Controls.Find(kvp.Key, true).FirstOrDefault();

                if (control is Button button)
                {
                    button.Text = kvp.Value;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.Text = kvp.Value;
                }
            }
        }


        private void UpdateButton14Translation()
        {
            button14.Text = _isPortuguese ? "Change language to english" : "Mudar idioma para português";
        }

        #endregion

        #region Tenchu 1 PS1

        #region Extrair VOL Tenchu 1
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo VOL|*.VOL|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo DATA.VOL do jogo Tenchu de PlayStation...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        const ulong MagicA = 6866950764218238529;   // "AFS_VOL_"
                        const uint MagicB = 3158066;   // "200\0"

                        ulong magicA = br.ReadUInt64();
                        uint magicB = br.ReadUInt32();

                        if (magicA != MagicA || magicB != MagicB)
                        {
                            throw new Exception("Arquivo inválido");
                        }

                        uint quantidadearquivos = Converterendian.bigendian32(br.ReadUInt32());

                        uint offsettabeladearquivos = Converterendian.bigendian32(br.ReadUInt32());

                        //Cria a pasta com o nome do arquivo aberto para salvar os arquivos dentro
                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                        //A variavel pasta recebe o caminho de onde a pasta foi criada
                        string pastabase = (Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                        Dictionary<int, string> pastas = new Dictionary<int, string>();

                        for (int i = 0; i < quantidadearquivos; i++)
                        {
                            br.BaseStream.Seek(offsettabeladearquivos + (i * 0x24), SeekOrigin.Begin);

                            uint ix = Converterendian.bigendian16(br.ReadUInt16()); //Always "IX"(0x4958)

                            uint tipoarquivo = Converterendian.bigendian16(br.ReadUInt16()); //is 0x0002 if the entry is a directory - is 0x0001 if the entry is a file

                            uint offsetarquivo = Converterendian.bigendian32(br.ReadUInt32()); //File offset - is 0xcdcdcdcd if the entry is a directory

                            uint tamanhoarquivo = 0;

                            uint tamanhoarquivo2 = 0;

                            if (tipoarquivo == 2)
                            {
                                br.BaseStream.Seek(8, SeekOrigin.Current);
                            }
                            else
                            {
                                tamanhoarquivo = Converterendian.bigendian32(br.ReadUInt32()); //File size - is 0xcdcdcdcd if the entry is a directory

                                tamanhoarquivo2 = Converterendian.bigendian32(br.ReadUInt32()); //Is also the file size? don't know why.
                            }

                            byte[] bnome = new byte[20]; //Array q vai ler o nome do arquivo ou pasta. São sempre 20 bytes pro nome

                            for (int j = 0; j < 20; j++)
                            {
                                bnome[j] = br.ReadByte();
                            }

                            string ascii = System.Text.Encoding.Default.GetString(bnome);

                            string nomearquivo = ascii.Replace(":", string.Empty).Replace("\0", string.Empty);

                            byte[] arquivoaserextraido = new byte[tamanhoarquivo];

                            String nomeDoArquivo = nomearquivo;

                            if (nomearquivo.Contains('_'))
                            {
                                //nomearquivo = nomearquivo.Substring(nomearquivo.IndexOf('_') + 1);
                                int indiceDiretorioPai = int.Parse(nomeDoArquivo.Substring(1, nomeDoArquivo.IndexOf('_') - 1));
                                nomearquivo = Path.Combine(pastas[indiceDiretorioPai], nomearquivo);
                            }

                            pastas.Add(i, nomearquivo);

                            if (tipoarquivo == 2) // Se for 2 é uma pasta
                            {
                                Directory.CreateDirectory(Path.Combine(pastabase, nomearquivo));
                            }
                            else if (tipoarquivo == 1) // Se for 1 é um arquivo
                            {
                                br.BaseStream.Seek(offsetarquivo, SeekOrigin.Begin);

                                arquivoaserextraido = br.ReadBytes((int)tamanhoarquivo);

                                //aqui já terminou de ler todos os textos, escreve o arquivo dumpado, dentro da pasta criada
                                File.WriteAllBytes(Path.Combine(pastabase, nomearquivo), arquivoaserextraido);
                            }
                        }

                        string[] linhas = pastas.Values.ToArray();
                        File.WriteAllLines(Path.Combine(pastabase, "VOLFileList.txt"), linhas);
                    }
                }
                MessageBox.Show("Terminado");
            }
        }
        #endregion

        #region Recriar VOL Tenchu 1
        private void button2_Click(object sender, EventArgs e)
        {
            //Recriar VOL
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo de Texto VOLFileList.txt|*.txt|All Files (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo VOLFileList.txt...";
            openFileDialog1.Multiselect = false;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog1.FileName;

                // abre o arquivo VOLFileList.txt para leitura
                StreamReader reader = new StreamReader(file);

                string pastabase = Path.GetDirectoryName(file);

                // cria o arquivo .VOL para escrita
                BinaryWriter bw = new BinaryWriter(File.Open(Path.Combine(pastabase, "DATA.VOL"), FileMode.Create));

                // escreve o cabeçalho do arquivo .VOL

                const ulong magicA = 6866950764218238529;   // "AFS_VOL_"
                const uint magicB = 3158066;   // "200\0"

                bw.Write((long)magicA); // MAGIC do arquivo .VOL
                bw.Write((int)magicB); // MAGIC do arquivo .VOL

                //Calcular o tamanho de todos os arquivos
                string[] listadosarquivos = File.ReadAllLines(file);
                uint numeroLinhas = (uint)listadosarquivos.Length;

                bw.Write(Converterendian.bigendian32(numeroLinhas)); // número de arquivos + pastas no .VOL - é assim no arquivo original

                long tamanhototaldosarquivos = 0;
                uint contador = 0;

                int[] tamanhoarquivo = new int[numeroLinhas];

                foreach (string caminhonotxt in listadosarquivos)
                {
                    FileInfo fi = new FileInfo(Path.Combine(pastabase, caminhonotxt));
                    if (fi.Exists)
                    {
                        tamanhototaldosarquivos += fi.Length;
                        tamanhoarquivo[contador] = (int)fi.Length;
                    }
                    else
                    {
                        //Define o valor 0xCDCDCDCD como tamanho sempre que for pasta
                        tamanhoarquivo[contador] = (int)Converterendian.bigendian32(0xCDCDCDCD);
                    }
                    contador++;
                }

                bw.Write(Converterendian.bigendian32((uint)tamanhototaldosarquivos + 0x28)); // offset do indice de arquivos no .VOL

                for (int padding = 0; padding < 0x14; padding++)
                {
                    bw.Write((byte)0);
                }

                contador = 0;

                foreach (string caminhonotxt in listadosarquivos)
                {
                    FileInfo fi = new FileInfo(Path.Combine(pastabase, caminhonotxt));
                    if (fi.Exists)
                    {
                        //byte[] conteudoarquivo = new byte[tamanhoarquivo[contador]];
                        //conteudoarquivo = File.ReadAllBytes(Path.Combine(pastabase, caminhonotxt));
                        byte[] conteudoarquivo = File.ReadAllBytes(Path.Combine(pastabase, caminhonotxt)); //Código do GDK
                        bw.Write(conteudoarquivo);
                    }
                    contador++;
                }

                //Vai pro offset pra escrever o indice
                bw.BaseStream.Seek(tamanhototaldosarquivos + 0x28, SeekOrigin.Begin);

                // inicializa o contador de arquivos
                contador = 0;

                // lê as linhas do arquivo VOLFileList.txt
                string line;

                int offset = 0x28;

                // Cria um dicionário para mapear nomes de arquivos/pastas aos índices de suas pastas pai
                Dictionary<string, int> tabelaDeArquivosEPastas = new Dictionary<string, int>();

                while ((line = reader.ReadLine()) != null) //Enquanto tiver linhas é diferente de nulo, quando terminar vai ser nulo
                {
                    // quebra a linha em partes separadas pelo caractere '\'
                    string[] parts = line.Split('\\'); //Código do ChatGPT

                    bw.Write((short)0x5849); // Sempre é IX

                    // escreve o tipo do arquivo e o offset/tamanho (duas vezes)
                    if (tamanhoarquivo[contador] != (int)Converterendian.bigendian32(0xCDCDCDCD))
                    {
                        bw.Write((ushort)Converterendian.bigendian16(1)); //Tipo 01 arquivo
                        bw.Write(Converterendian.bigendian32((uint)offset));
                        bw.Write(Converterendian.bigendian32((uint)tamanhoarquivo[contador]));
                        bw.Write(Converterendian.bigendian32((uint)tamanhoarquivo[contador]));
                        offset += tamanhoarquivo[contador];
                    }
                    else
                    {
                        bw.Write((ushort)Converterendian.bigendian16(2)); //Tipo 02 pasta
                        bw.Write(Converterendian.bigendian32(0xCDCDCDCD));
                        bw.Write(Converterendian.bigendian32(0xCDCDCDCD));
                        bw.Write(Converterendian.bigendian32(0xCDCDCDCD));
                    }

                    // define o nome do arquivo (com 20 bytes de tamanho) //Código do ChatGPT
                    string filename = parts[parts.Length - 1];
                    if (contador == 0)
                    {
                        filename += ":";
                    }
                    filename = filename.PadRight(20, '\0');
                    bw.Write(Encoding.Default.GetBytes(filename)); //Código do ChatGPT

                    // incrementa o contador de arquivos
                    contador++;
                }
                // fecha o arquivo .vol
                bw.Close(); //Código do ChatGPT

                // fecha o arquivo VOLFileList.txt
                reader.Close(); //Código do ChatGPT

                MessageBox.Show("Terminado", "AVISO!");
            }
        }

        #endregion

        #region Extrair Texto Tenchu 1
        private void button3_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu 1|*.CAD|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo do jogo Tenchu de PlayStation...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        long filesize = new FileInfo(file).Length;

                        int pointerquantity = 0;

                        for (int i = 0; i < filesize - 10; i++)
                        {
                            int test = br.ReadInt16();
                            i++;

                            if (test == 0x08)
                            {
                                int test2 = br.ReadInt16();
                                i++;
                                i++;

                                if (test2 < filesize & test2 > test)
                                {
                                    pointerquantity++;
                                }
                            }
                        }

                        br.BaseStream.Seek(0, SeekOrigin.Begin);

                        int[] offsetpointeirs = new int[pointerquantity];

                        int a = 0;

                        for (int i = 0; i < filesize - 10; i++)
                        {
                            int test = br.ReadInt16();
                            i++;

                            if (test == 0x08)
                            {
                                int test2 = br.ReadInt16();
                                i++;
                                i++;

                                if (test2 < filesize & test2 > test)
                                {
                                    offsetpointeirs[a] = i - 1;
                                    a++;
                                }
                            }
                        }

                        int[] pointer = new int[pointerquantity];

                        for (int i = 0; i < pointerquantity; i++)
                        {
                            br.BaseStream.Seek(offsetpointeirs[i], SeekOrigin.Begin);

                            pointer[i] = br.ReadInt16();
                        }

                        string alltexts = "";

                        int[] textsize = new int[pointerquantity];


                        for (int b = 0; b < pointerquantity; b++)
                        {
                            if (b < pointerquantity - 1)
                            {
                                textsize[b] = pointer[b + 1] - pointer[b];
                            }
                            else
                            {
                                textsize[b] = (int)(filesize - pointer[b]);
                            }

                            br.BaseStream.Seek(pointer[b], SeekOrigin.Begin);

                            byte[] bytestext = new byte[textsize[b]];

                            for (int j = 0; j < textsize[b]; j++)
                            {
                                bytestext[j] = br.ReadByte();
                            }

                            string decodedText = "";

                            if (checkBox1.Checked)
                            {
                                decodedText = Encoding.GetEncoding("shift_jis").GetString(bytestext);
                            }
                            else
                            {
                                decodedText = Encoding.Default.GetString(bytestext);
                            }

                            alltexts += decodedText.Replace("\0", String.Empty) + "\r\n";

                            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt", alltexts);
                        }
                    }
                }
                MessageBox.Show("Text extracted!\nTexto extraido!", "Warning! / AVISO!");
            }
        }
        #endregion

        #region Inserir Texto Tenchu 1
        private void button4_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu 1|*.CAD|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo do jogo Tenchu de PlayStation...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        FileInfo dump = new FileInfo(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt");

                        string filename = Path.GetFileNameWithoutExtension(file);

                        if (dump.Exists)
                        {
                            var txt = File.ReadLines(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt");

                            long filesize = new FileInfo(file).Length;

                            int pointerquantity = 0;

                            for (int i = 0; i < filesize - 10; i++)
                            {
                                int test = br.ReadInt16();
                                i++;

                                if (test == 0x08)
                                {
                                    int test2 = br.ReadInt16();
                                    i++;
                                    i++;

                                    if (test2 < filesize & test2 > test)
                                    {
                                        pointerquantity++;
                                    }
                                }
                            }

                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            int[] offsetpointeirs = new int[pointerquantity];

                            int a = 0;

                            for (int i = 0; i < filesize - 10; i++)
                            {
                                int test = br.ReadInt16();
                                i++;

                                if (test == 0x08)
                                {
                                    int test2 = br.ReadInt16();
                                    i++;
                                    i++;

                                    if (test2 < filesize & test2 > test)
                                    {
                                        offsetpointeirs[a] = i - 1;
                                        a++;
                                    }
                                }
                            }

                            int[] pointer = new int[pointerquantity];

                            for (int i = 0; i < pointerquantity; i++)
                            {
                                br.BaseStream.Seek(offsetpointeirs[i], SeekOrigin.Begin);

                                pointer[i] = br.ReadInt16();
                            }

                            stream.SetLength(pointer.First());

                            int numberline = 0;

                            int newpointer = pointer[0];

                            try
                            {
                                foreach (var line in txt)
                                {
                                    bw.BaseStream.Seek(offsetpointeirs[numberline], SeekOrigin.Begin);

                                    bw.Write((short)newpointer);

                                    byte[] bytes = Encoding.Default.GetBytes(line);

                                    bw.BaseStream.Seek(newpointer, SeekOrigin.Begin);

                                    bw.Write(bytes);

                                    newpointer += bytes.Length + 1;

                                    numberline++;
                                }
                            }
                            catch (System.IndexOutOfRangeException)
                            {

                            }
                            bw.Write((byte)0);
                        }
                        else
                        {
                            //Avisa que não encontrou o arquivo e encerra o programa
                            MessageBox.Show(filename + ".txt file not found!\n\nO arquivo " + filename + ".txt não foi encontrado!", "Warning! / AVISO!");

                            return; //Volta pro programa
                        }
                    }
                }
                MessageBox.Show("Text inserted!\n\nTexto inserido!", "Warning! / AVISO!");
            }
        }

        #endregion

        #region Visualizador de TIM
        private void timbutton_Click(object sender, EventArgs e)
        {
            this.Hide(); //Esconde o formulário principal
            TIMT timtForm = new TIMT();
            timtForm.FormClosed += (s, args) => this.Show(); //Fecha o programa se fechar o formulário
            timtForm.Show(); //Mostra o formulário
        }

        #endregion

        #endregion

        #region Tenchu 2 PS1

        #region Extrair DAT Tenchu 2
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu 2|*.DAT|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo do jogo Tenchu 2 de PlayStation...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        //Cria a pasta com o nome do arquivo aberto para salvar os arquivos dentro
                        Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                        //A variavel pasta recebe o caminho de onde a pasta foi criada
                        string pastabase = (Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                        //000h 4     Unknown(demo = A0409901h, us / retail = A0617023h)
                        int desconhecido = br.ReadInt32();

                        //004h 4     Unknown(0h)                        
                        int desconhecido2 = br.ReadInt32();

                        //008h 4     Number of files(F)(demo = B7h, us / retail = 1294h)                        
                        int numerodearquivos = br.ReadInt32();

                        //00Ch 4     Number of folders(D)(demo = 0Fh, us / retail = 3Eh)
                        int numerodepastas = br.ReadInt32();

                        //010h D*8   Folder List
                        //000h 4     Folder ID(Random, maybe folder name checksum?)
                        int[] nomepasta = new int[numerodepastas];

                        //004h 4     First file number in this folder (0=first, increasing)
                        int[] primeironumeroarquivo = new int[numerodepastas];

                        string pasta = "";

                        for (int i = 0; i < numerodepastas; i++)
                        {
                            nomepasta[i] = br.ReadInt32();
                            primeironumeroarquivo[i] = br.ReadInt32();

                            pasta = nomepasta[i].ToString();

                            if (!Directory.Exists(Path.Combine(pastabase, pasta)))
                            {
                                Directory.CreateDirectory(Path.Combine(pastabase, pasta));
                            }
                        }

                        stream.Seek(0x800, SeekOrigin.Begin);

                        int[] offsetarquivo = new int[numerodearquivos];
                        int[] tamanhoarquivo = new int[numerodearquivos];
                        int[] pastadestino = new int[numerodearquivos];
                        int[] idarquivo = new int[numerodearquivos];

                        for (int i = 0; i < numerodearquivos; i++)
                        {
                            offsetarquivo[i] = br.ReadInt32() * 0x800;
                            tamanhoarquivo[i] = br.ReadInt32();
                            pastadestino[i] = br.ReadInt32();
                            idarquivo[i] = br.ReadInt32();
                        }

                        for (int i = 0; i < numerodearquivos; i++)
                        {
                            stream.Seek(offsetarquivo[i], SeekOrigin.Begin);

                            // Leia os dados do arquivo
                            byte[] dadosdoarquivo = new byte[tamanhoarquivo[i]];
                            stream.Read(dadosdoarquivo, 0, tamanhoarquivo[i]);

                            // Salve o arquivo em um arquivo externo
                            string nomearquivo = idarquivo[i].ToString();
                            string pastafinal = pastadestino[i].ToString();
                            File.WriteAllBytes(Path.Combine(pastabase, pastafinal, nomearquivo), dadosdoarquivo);
                        }

                        stream.Close();

                        MessageBox.Show("Terminado");

                    }
                }
            }
        }
        #endregion

        #region Recriar DAT Tenchu 2
        private void button6_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region Extrair texto Tenchu 2
        private void button7_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not ready yet!\n\nAinda não está pronto!", "AVISO!");
        }
        #endregion

        #region Inserir texto Tenchu 2
        private void button8_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Not ready yet!\n\nAinda não está pronto!", "AVISO!");
        }
        #endregion

        #endregion

        #region Tenchu PS2

        #region Extrator Texto PS2
        private void button9_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu|SLUS_211.29; *.TCD|All files (*.*)|*.*";
            openFileDialog1.Title = "Select a Tenchu File...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        int magic = br.ReadInt32();

                        if (magic == 0x464C457F) // ELF - SLUS_211.29
                        {
                            const int totaldetextos = 18;

                            int offsettexto = 0x1B3A20;

                            int offsetponteiro = 0x1B4270;

                            br.BaseStream.Seek(offsetponteiro, SeekOrigin.Begin);

                            int[] ponteiros = new int[18];

                            for (int i = 0; i < totaldetextos; i++)
                            {
                                ponteiros[i] = br.ReadInt32();
                            }

                            int[] tamanhotexto = new int[totaldetextos];

                            for (int i = 0; i < totaldetextos; i++)
                            {
                                if (i < 17)
                                {
                                    tamanhotexto[i] = ponteiros[i + 1] - ponteiros[i];
                                }
                                else
                                {
                                    tamanhotexto[i] = 0x70;
                                }
                            }

                            string todosOsTextos = "";

                            br.BaseStream.Seek(offsettexto, SeekOrigin.Begin);

                            for (int i = 0; i < totaldetextos; i++)
                            {
                                byte[] texto = new byte[tamanhotexto[i]];

                                for (int j = 0; j < tamanhotexto[i]; j++)
                                {
                                    texto[j] = br.ReadByte();
                                }

                                string ascii = System.Text.Encoding.Default.GetString(texto);

                                ascii = ascii.Replace("\0\0\0", String.Empty).Replace("\0", "<line>"); ;

                                todosOsTextos += ascii + "<end>\r\n\r\n";
                            }
                            string resultado = todosOsTextos.Replace("<line><line><end>", "<end>").Replace("<line><end>", "<end>");

                            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".txt", resultado);
                        }

                        else if (magic == 0x74444354) // Arquivo TCD
                        {
                            //Pega o tamanho do arquivo
                            long tamanhoarquivo = new FileInfo(file).Length;

                            //Vai pro offset 0
                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            int totaldeTCDt = 0;

                            //Laço que conta o número de arquivos TCDt dentro do conteiner TCD
                            for (int a = 0; a < tamanhoarquivo / 4 - 200; a++)
                            {
                                int tCDt = br.ReadInt32();

                                if (tCDt == 0x74444354)
                                {
                                    totaldeTCDt++;
                                }
                                else
                                {
                                    totaldeTCDt += 0;
                                }
                            }

                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            int[] enderecoTCDt = new int[totaldeTCDt + 1];

                            int b = 1;

                            //Laço que guarda os endereços de começo de cada arquivo TCDt dentro do conteiner TCD
                            for (int d = 0; d < tamanhoarquivo / 4 - 200; d++)
                            {
                                int tCDt = br.ReadInt32();

                                if (tCDt == 0x74444354)
                                {
                                    enderecoTCDt[b] = d * 4;
                                    b++;
                                }
                            }

                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            //Laço que vai ler todo o TCD e extrair
                            for (int a = 1; a <= totaldeTCDt; a++)
                            {
                                br.BaseStream.Seek(enderecoTCDt[a], SeekOrigin.Begin);

                                magic = br.ReadInt32();

                                int quantidadedeblocos = br.ReadInt32();

                                br.BaseStream.Seek(0x10 + enderecoTCDt[a], SeekOrigin.Begin);

                                int[] enderecoblocos = new int[quantidadedeblocos + 1];

                                for (int c = 1; c <= quantidadedeblocos; c++)
                                {
                                    enderecoblocos[c] = br.ReadInt32();
                                }

                                for (int bloco = 1; bloco <= quantidadedeblocos; bloco++)
                                {
                                    br.BaseStream.Seek(enderecoTCDt[a] + enderecoblocos[bloco], SeekOrigin.Begin);

                                    int quantidadetextos = br.ReadInt32();

                                    int ponteiro = br.ReadInt32(); //Lê o valor do primeiro ponteiro

                                    int teste = ponteiro + enderecoTCDt[a] + enderecoblocos[bloco];

                                    br.BaseStream.Seek(teste, SeekOrigin.Begin);

                                    string todosOsTextos = "";

                                    for (int i = 1; i <= quantidadetextos; i++)
                                    {
                                        //Inicia a variavel que vai verificar se o texto acabou ou não
                                        bool acabouotexto = false;

                                        //Equanto não acabar o texto ele vai repetindo

                                        while (acabouotexto == false)
                                        {
                                            int comparador;

                                            string convertido;

                                            //Lê um byte do texto
                                            comparador = br.ReadByte();

                                            //compara se o byte é a endstring
                                            //se for, o programa cria uma nova linha
                                            //se não continua pra proxima letra
                                            if (comparador == 0x00)
                                            {
                                                comparador = br.ReadByte();

                                                if (comparador == 0x00)
                                                {
                                                    //Quando chegar em uma endstring ele retorna como o texto tendo acabado (acabouotexto = verdadeiro)
                                                    acabouotexto = true;

                                                    todosOsTextos += "<end>\r\n<00>";

                                                    br.BaseStream.Seek(-1, SeekOrigin.Current);

                                                    //Volta pra ler o próximo ponteiro
                                                    br.BaseStream.Seek(enderecoTCDt[a] + enderecoblocos[bloco] + 4 + i * 4, SeekOrigin.Begin);

                                                    //Lê o ponteiro
                                                    ponteiro = br.ReadInt32();

                                                    if (i == quantidadetextos)
                                                    {
                                                        //Não faz nada
                                                    }
                                                    else
                                                    {
                                                        br.BaseStream.Seek(ponteiro + enderecoTCDt[a] + enderecoblocos[bloco], SeekOrigin.Begin);
                                                    }
                                                }
                                                else if (comparador != 0x00)
                                                {
                                                    br.BaseStream.Seek(-1, SeekOrigin.Current);
                                                    //Quando chegar em uma endstring ele retorna como o texto tendo acabado (acabouotexto = verdadeiro)
                                                    acabouotexto = true;
                                                    todosOsTextos += "<end>\r\n";

                                                    //Volta pra ler o próximo ponteiro
                                                    br.BaseStream.Seek(enderecoTCDt[a] + enderecoblocos[bloco] + 4 + i * 4, SeekOrigin.Begin);

                                                    //Lê o ponteiro
                                                    ponteiro = br.ReadInt32();
                                                    if (i == quantidadetextos)
                                                    {
                                                        //Não faz nada
                                                    }
                                                    else
                                                    {
                                                        br.BaseStream.Seek(ponteiro + enderecoTCDt[a] + enderecoblocos[bloco], SeekOrigin.Begin);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                acabouotexto = false;

                                                //Começa a conversão dos caracteres
                                                convertido = tabela.Converterascii(comparador);

                                                todosOsTextos += convertido;
                                            }
                                        }
                                    }
                                    //Cria a pasta com o nome do arquivo aberto para salvar os arquivos dentro
                                    Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                                    //A variavel pasta recebe o caminho de onde a pasta foi criada
                                    string pasta = (Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                                    string nomedoarquivo = "TCDT_" + a + "_bloco_" + bloco + ".txt";

                                    //aqui já terminou de ler todos os textos, escreve o arquivo dumpado, dentro da pasta criada
                                    //File.WriteAllBytes(Path.Combine(pasta, Path.GetFileName(nomedoarquivo)), arquivoaserextraido);
                                    File.WriteAllText(Path.Combine(pasta, pasta, nomedoarquivo), todosOsTextos);
                                }
                            }
                        }
                    }
                }
                //Avisa que a extração dos textos terminou
                MessageBox.Show("Text extracted!\n\nTexto Extraido!", "AVISO!");
            }
        }
        #endregion

        #region Insersor Texto PS2
        private void button10_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu|SLUS_211.29; *.TCD|All Files (*.*)|*.*";
            openFileDialog1.Title = "Select a Tenchu File...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        br.BaseStream.Seek(0x00, SeekOrigin.Begin);

                        int magic = br.ReadInt32(); // Read magic

                        if (magic == 0x464C457F) // ELF
                        {
                            int offsettexto = 0x1B3A20;

                            int offsetponteiros = 0x1B4270;

                            FileInfo dump = new FileInfo(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".txt");

                            string nomearquivo = Path.GetFileName(file);

                            if (dump.Exists)
                            {
                                bw.BaseStream.Seek(offsettexto, SeekOrigin.Begin);

                                for (int i = 0; i < offsetponteiros - offsettexto; i++)
                                {
                                    bw.Write((byte)0);
                                }

                                var txt = File.ReadLines(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".txt");

                                br.BaseStream.Seek(offsetponteiros, SeekOrigin.Begin);

                                int ponteiro = br.ReadInt32();

                                int tamanhosubtracao = ponteiro - offsettexto;

                                int novoponteiro = ponteiro;

                                int numeroLinha = 0;

                                foreach (var linha in txt)
                                {
                                    bw.BaseStream.Seek(offsetponteiros + numeroLinha * 4, SeekOrigin.Begin);

                                    bw.Write(novoponteiro);

                                    string texto = linha.Replace("<line>", "\0").Replace("<end>", string.Empty);

                                    byte[] bytes = Encoding.Default.GetBytes(texto);

                                    bw.BaseStream.Seek(novoponteiro - tamanhosubtracao, SeekOrigin.Begin);

                                    bw.Write(bytes);

                                    novoponteiro += Pad.pad16(bytes.Length + 1);

                                    numeroLinha++;
                                }
                            }
                            else
                            {
                                //Avisa que não encontrou o arquivo e encerra o programa
                                MessageBox.Show(nomearquivo + ".txt file not found!\n\nO arquivo " + nomearquivo + ".txt não foi encontrado!", "Warning! / AVISO!");

                                return; //Volta pro programa
                            }
                        }

                        else if (magic == 0x74444354) // Arquivo TCD
                        {
                            string nomepasta = Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file));

                            if (Directory.Exists(nomepasta))
                            {
                                br.BaseStream.Seek(0, SeekOrigin.Begin);

                                int totaldeTCDt = 0;

                                //Pega o tamanho do arquivo
                                long tamanhoarquivo = new FileInfo(file).Length;

                                //Laço que conta o número de arquivos TCDt dentro do conteiner TCD
                                for (int a = 0; a < tamanhoarquivo / 4 - 10; a++)
                                {
                                    int tCDt = br.ReadInt32();

                                    if (tCDt == 0x74444354)
                                    {
                                        totaldeTCDt++;
                                    }
                                    else
                                    {
                                        totaldeTCDt += 0;
                                    }
                                }

                                br.BaseStream.Seek(0, SeekOrigin.Begin);

                                int[] enderecotcdt = new int[totaldeTCDt + 1];

                                int b = 1;

                                int[] quantidadeblocos = new int[totaldeTCDt + 1];

                                //Laço que guarda os endereços de começo de cada arquivo TCDt dentro do conteiner TCD
                                for (int i = 0; i < tamanhoarquivo / 4 - 100; i++)
                                {
                                    int tCDt = br.ReadInt32();

                                    if (tCDt == 0x74444354)
                                    {
                                        quantidadeblocos[b] = br.ReadInt32();
                                        enderecotcdt[b] = i * 4;
                                        b++;
                                    }
                                }

                                stream.SetLength(0); //Apaga todo o arquivo

                                int novotamanhotcdt = 0; //Tamanho do arquivo TCDt

                                for (int x = 1; x <= totaldeTCDt; x++)
                                {
                                    br.BaseStream.Seek(novotamanhotcdt, SeekOrigin.Begin); //Vai pro inicio do TCDt

                                    bw.Write(0x74444354); //Escreve o Magic - TCDt

                                    bw.Write(quantidadeblocos[x]); //Escreve quantos blocos de texto tem no TCDt

                                    bw.Write(0xFFFFFFFF); //Faz o primeiro padding, pois o cabeçalho tem 0x10 de tamanho
                                    bw.Write(0xFFFFFFFF); //Faz o segundo padding, pois o cabeçalho tem 0x10 de tamanho

                                    //variavel que vai receber o tamanho do espaço ocupado por todos os endereços dos blocos
                                    int bytesquantidadedeblocos = Pad.pad16(quantidadeblocos[x] * 4) + 0x10;

                                    bw.Write(bytesquantidadedeblocos); //Escreve o endereço de começo do primeiro bloco de texto dentro do TCDt

                                    for (int f = 0; f <= bytesquantidadedeblocos - 0x10; f++) //Faz o preenchimento com FF do espaço onde vão ficar os endereços dos blocos
                                    {
                                        bw.Write((byte)0xFF);
                                    }

                                    long tamanhotxt = 0; //Inicia a variavel que vai receber o tamanho do TXT com os textos extraidos

                                    int tamanhobytesponteiros = 0; //Inicia a variavel que vai receber o tamanho do espaço ocupado por todos os ponteiros

                                    for (int i = 1; i <= quantidadeblocos[x]; i++) //Inicia o laço pra ler os blocos e inserir
                                    {
                                        FileInfo fi = new FileInfo(Path.Combine(nomepasta, "TCDT_" + x + "_bloco_" + i + ".txt")); //fi recebe o nome do arquivo txt pra conferir se o arquivo existe

                                        string arquivotxt = Path.Combine(nomepasta, "TCDT_" + x + "_bloco_" + i + ".txt"); //Recebe o nome do arquivo txt pra facilitar na abertura do arquivo

                                        if (fi.Exists) //Se o arquivo existir, o programa continua, caso contrario mostra a mensagem de arquivo não encontrado
                                        {
                                            string todosostextos = File.ReadAllText(arquivotxt); //Lê todo o texto do arquivo para a variavel todos os textos
                                            string tamanhotextos = todosostextos.Replace("<end>\r\n", string.Empty).Replace("<00>", "\0"); //Apaga da variavel <end> e substitui <00> por 0

                                            int quantidadezero = tamanhotextos.Split(new char[] { '\0' }).Length - 1;

                                            var linhasdetextos = File.ReadLines(arquivotxt); //A variavel linhasdetextos recebe os textos de todas as linha do arquivo txt
                                            var quantidadeponteiros = File.ReadAllLines(arquivotxt).Length; //A variavel txt recebe a quantidade de linhas no arquivo txt

                                            //novoponteiro recebe a quatidade de linhas (ponteiros) e multiplica por 4 pra ter o valor do primeiro ponteiro
                                            //Faz o padding como no arquivo original e coloquei + 1 que é o byte que guarda a quantidade de ponteiros
                                            //O valor do ponteiro não inclui o cabeçalho e nem o endereço dos blocos
                                            int novoponteiro = Pad.pad16((quantidadeponteiros * 4) + 1);

                                            //Aqui faço o calculo pra ir pra onde o programa deve começar a escrever os textos
                                            int offsettextos = bytesquantidadedeblocos + tamanhobytesponteiros + novoponteiro + (int)tamanhotxt + novotamanhotcdt;

                                            int offsetquantidadeponteiros = bytesquantidadedeblocos + (int)tamanhotxt + novotamanhotcdt + tamanhobytesponteiros;

                                            bw.BaseStream.Seek(offsetquantidadeponteiros, SeekOrigin.Begin);

                                            int ff = Pad.pad16((quantidadeponteiros * 4) + 1);

                                            for (int f = 1; f <= ff; f++)
                                            {
                                                bw.Write((byte)0xFF);
                                            }

                                            bw.BaseStream.Seek(offsettextos, SeekOrigin.Begin); //Vai pra onde deve começar a escrever os textos

                                            ff = Pad.pad16(tamanhotextos.Length + quantidadeponteiros - quantidadezero);

                                            for (int f = 1; f <= ff; f++)
                                            {
                                                bw.Write((byte)0xFF);
                                            }

                                            bw.BaseStream.Seek(offsetquantidadeponteiros, SeekOrigin.Begin);

                                            bw.Write(quantidadeponteiros);

                                            int numerolinha = 1; //Inicia a variavel pra contar as linhas e auxilixar na escrita dos ponteiros

                                            foreach (var linha in linhasdetextos)
                                            {
                                                int offsetnovoponteiro = bytesquantidadedeblocos + (int)tamanhotxt + novotamanhotcdt + numerolinha * 4 + tamanhobytesponteiros;

                                                bw.BaseStream.Seek(offsetnovoponteiro, SeekOrigin.Begin);

                                                bw.Write(novoponteiro);

                                                string texto = linha.Replace("<end>", string.Empty).Replace("<00>", string.Empty);

                                                byte[] bytes = Encoding.Default.GetBytes(texto);

                                                offsettextos = bytesquantidadedeblocos + (int)tamanhotxt + novotamanhotcdt + novoponteiro + tamanhobytesponteiros;

                                                bw.BaseStream.Seek(offsettextos, SeekOrigin.Begin);

                                                bw.Write(bytes);

                                                bw.Write((byte)0);

                                                novoponteiro += bytes.Length + 1;

                                                numerolinha++;
                                            }

                                            tamanhotxt += Pad.pad16(tamanhotextos.Length + quantidadeponteiros - quantidadezero);

                                            tamanhobytesponteiros += Pad.pad16((quantidadeponteiros * 4) + 1);

                                            if (i < quantidadeblocos[x])
                                            {
                                                //Vai pro começo escrever o endereço dos blocos de textos
                                                bw.BaseStream.Seek(0x10 + i * 4 + novotamanhotcdt, SeekOrigin.Begin);

                                                int novoenderecobloco = Pad.pad16((int)tamanhotxt + tamanhobytesponteiros + bytesquantidadedeblocos);

                                                //Escreve o endereço de começo do bloco
                                                bw.Write(novoenderecobloco);
                                            }
                                        }
                                        else
                                        {
                                            //Avisa q não foi encontrado o arquivo txt
                                            MessageBox.Show("File " + fi + " not found!\n\nArquivo " + fi + " não encontrado!", "Warning! / AVISO!");
                                            return;
                                        }
                                    }
                                    novotamanhotcdt += Pad.pad800((int)tamanhotxt + tamanhobytesponteiros);
                                }
                            }

                            else
                            {
                                //Avisa que não encontrou a pasta onde os txt devem estar dentro
                                MessageBox.Show("Directory " + nomepasta + " not found!\n\nPasta " + nomepasta + " não encontrada!", "Warning! / AVISO!");
                                return; //Volta pro programa
                            }
                        }
                    }

                    using (FileStream stream = File.Open(file, FileMode.Open)) //Abre o arquivo
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        br.BaseStream.Seek(0x00, SeekOrigin.Begin); //Vai pra posição 0

                        int magic = br.ReadInt32(); // Lê o magic do arquivo

                        if (magic == 0x74444354) // Arquivo TCD
                        {
                            FileInfo fi = new FileInfo(file);  //Verifica o tamanho do arquivo em bytes
                            long tamanhototalarquivo = (fi.Length); //Pega o tamanho e armazena na variavel tamanhototalarquivo
                            stream.SetLength(Pad.pad800long(tamanhototalarquivo)); //Cria os 0x00 de padding no final do arquivo de PS2
                        }
                    }
                }
                //Avisa que terminou
                MessageBox.Show("Text Inserted!\n\nTexto Inserido!", "Warning! / AVISO!");
            }
        }
        #endregion

        #region Texturas Tenchu PS2

        #region Visualizador de Texturas Tenchu PS2

        private Dictionary<string, List<Bitmap>> binImages = new Dictionary<string, List<Bitmap>>();

        private void button17_Click(object sender, EventArgs e)
        {
            this.Hide(); //Esconde o formulário principal
            Form2 form2 = new Form2(); //Define form2 como o formulário de visualização e extração gráfica do PS2
            form2.FormClosed += (s, args) => this.Show(); //Fecha o programa se fechar o formulário
            form2.Show(); //Mostra o formulário
        }

        #endregion

        #region Extrair todas as texturas de uma vez PS2

        private async void button18_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Arquivo Tenchu|*.bin|All files (*.*)|*.*";
            openFileDialog1.Title = "Select a Tenchu File...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                await Task.Run(() =>
                {
                    foreach (String file in openFileDialog1.FileNames)
                    {
                        // Salvar a imagem como PNG
                        // Gerar nome da pasta com base no nome do arquivo original
                        string folderName = Path.GetFileNameWithoutExtension(file);
                        string folderPath = Path.Combine(Path.GetDirectoryName(file), folderName);

                        // Verificar se a pasta existe, se não, criar
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        using (FileStream stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (BinaryReader br = new BinaryReader(stream))
                        {
                            // Pega o tamanho do arquivo
                            long tamanhoarquivo = new FileInfo(file).Length;

                            // Vai pro offset 0
                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            int totaldeK2Tx = 0;
                            List<int> enderecoK2TxList = new List<int>();

                            // Laço que conta o número de arquivos K2Tx dentro do contêiner e guarda os endereços
                            for (int a = 0; a < tamanhoarquivo / 4 - 200; a++)
                            {
                                int K2Tx = br.ReadInt32();

                                if (K2Tx == 0x7854324B) // K2Tx
                                {
                                    totaldeK2Tx++;
                                    enderecoK2TxList.Add(a * 4);
                                }
                            }

                            // Converte a lista de endereços para um array
                            int[] enderecoK2Tx = enderecoK2TxList.ToArray();

                            for (int i = 0; i < totaldeK2Tx; i++)
                            {
                                br.BaseStream.Seek(enderecoK2Tx[i], SeekOrigin.Begin);

                                int magic = br.ReadInt32();

                                int offsetdaimagem = br.ReadInt32(); // Le o endereço de onde começa a primeira textura
                                int offsetdapaleta = br.ReadInt32(); //Le o offset da paleta
                                int tamanhoimagem = br.ReadInt32(); //Le o tamanho do arquivo
                                ushort largura = br.ReadUInt16(); //Le a largura da imagem
                                ushort altura = br.ReadUInt16(); //Le a altura da imagem
                                ushort desconhecido1 = br.ReadUInt16();
                                ushort desconhecido2 = br.ReadUInt16();
                                ushort desconhecido3 = br.ReadUInt16();
                                ushort desconhecido4 = br.ReadUInt16(); //Sempre é 05 suspeito que seja a informação de 8bpp
                                ushort desconhecido5 = br.ReadUInt16();
                                ushort id = br.ReadUInt16(); //Me parece ser um ID

                                // Ler a imagem
                                br.BaseStream.Seek(offsetdaimagem + enderecoK2Tx[i], SeekOrigin.Begin);
                                byte[] imagemBytes = br.ReadBytes(tamanhoimagem - offsetdaimagem);

                                // Ler a paleta
                                br.BaseStream.Seek(offsetdapaleta + enderecoK2Tx[i], SeekOrigin.Begin);
                                byte[] paletaBytes = br.ReadBytes(tamanhoimagem - offsetdapaleta);


                                if (offsetdaimagem == 0x80)
                                {
                                    br.BaseStream.Seek(enderecoK2Tx[i] + 0x20, SeekOrigin.Begin);

                                    int verificador1 = br.ReadInt32();
                                    int verificador2 = br.ReadInt32();

                                    if ((verificador1 == 33554432 && verificador2 == 512) || (verificador1 == 16777216 && verificador2 == 256) || (verificador1 == 8388608 && verificador2 == 128) || (verificador1 == 4194304 && verificador2 == 64) || (verificador1 == 33554432 && verificador2 == 128))
                                    {
                                        // "UnSwizzle" para 8bpp
                                        byte[] unswizzled = new byte[largura * altura];
                                        for (int y = 0; y < altura; y++)
                                        {
                                            for (int x = 0; x < largura; x++)
                                            {
                                                int block_location = (y & (~0xf)) * largura + (x & (~0xf)) * 2;
                                                int swap_selector = (((y + 2) >> 2) & 0x1) * 4;
                                                int posY = (((y & (~3)) >> 1) + (y & 1)) & 0x7;
                                                int column_location = posY * largura * 2 + ((x + swap_selector) & 0x7) * 4;
                                                int byte_num = ((y >> 1) & 1) + ((x >> 2) & 2);
                                                int swizzleid = block_location + column_location + byte_num;

                                                unswizzled[y * largura + x] = imagemBytes[swizzleid];
                                            }
                                        }
                                        imagemBytes = unswizzled;
                                    }
                                }

                                // Processo de "unswizzle" da paleta
                                byte[] unswizzledPalette = new byte[1024];
                                for (int p = 0; p < 256; p++)
                                {
                                    int pos = ((p & 231) + ((p & 8) << 1) + ((p & 16) >> 1));
                                    if (p * 4 + 4 <= paletaBytes.Length && pos * 4 + 4 <= unswizzledPalette.Length)
                                    {
                                        Buffer.BlockCopy(paletaBytes, p * 4, unswizzledPalette, pos * 4, 4);
                                    }
                                }

                                // Criar um bitmap para a imagem
                                Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);

                                // Preencher o bitmap com os dados lidos
                                for (int y = 0; y < altura; y++)
                                {
                                    for (int x = 0; x < largura; x++)
                                    {
                                        int pixelIndex = y * largura + x;
                                        if (pixelIndex < imagemBytes.Length)
                                        {
                                            byte colorIndex = imagemBytes[pixelIndex];
                                            int paletteOffset = colorIndex * 4;
                                            if (paletteOffset + 4 <= unswizzledPalette.Length)
                                            {
                                                byte r = unswizzledPalette[paletteOffset];
                                                byte g = unswizzledPalette[paletteOffset + 1];
                                                byte b = unswizzledPalette[paletteOffset + 2];
                                                byte a = unswizzledPalette[paletteOffset + 3];
                                                byte adjustedAlpha = (byte)Math.Min(a * 2, 255); // Com ajuste do Alpha

                                                Color color = Color.FromArgb(adjustedAlpha, r, g, b);
                                                bitmap.SetPixel(x, y, color);
                                            }
                                        }
                                    }
                                }

                                // Gerar nome do arquivo com base no nome original e offset
                                string outputFileName = $"{folderName}_{enderecoK2Tx[i]}";
                                string outputFilePath = Path.Combine(folderPath, outputFileName + ".png");

                                // Salvar a imagem como PNG
                                bitmap.Save(outputFilePath, ImageFormat.Png);
                            }
                        }
                    }
                });
                MessageBox.Show("Terminado!");
            }
        }

        #endregion

        #endregion

        #endregion

        #region Dark Secrets
        private void button11_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo Tenchu|*.FARC|All files (*.*)|*.*";
            openFileDialog1.Title = "Select a Tenchu File...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        int magic = br.ReadInt32();

                        if (magic == 0x02314442) // BD1.
                        {
                            br.BaseStream.Seek(0x10, SeekOrigin.Begin);

                            int quantidade = br.ReadInt32();

                            int[] endereco = new int[quantidade];
                            int[] tamanho = new int[quantidade];

                            for (int i = 0; i < quantidade; i++)
                            {
                                endereco[i] = br.ReadInt32();
                                tamanho[i] = br.ReadInt32();
                            }

                            //Cria a pasta com o nome do arquivo aberto para salvar os arquivos dentro
                            Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                            //A variavel pasta recebe o caminho de onde a pasta foi criada
                            string pasta = (Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)));

                            for (int i = 0; i < quantidade; i++)
                            {
                                br.BaseStream.Seek(endereco[i], SeekOrigin.Begin);

                                int extensao = br.ReadInt32();

                                string ext;

                                switch (extensao)
                                {
                                    case 0x30414342: //BCA0
                                        ext = "NSBCA";
                                        break;

                                    case 0x02314442: //BCA\2
                                        ext = "FARC";
                                        break;

                                    case 0x30414D42: //BMA0
                                        ext = "NSBMA";
                                        break;

                                    case 0x30444D42: //BMD0
                                        ext = "NSBMD";
                                        break;

                                    case 0x30415442: //BTA0
                                        ext = "NSBTA";
                                        break;

                                    case 0x30505442: //BTP0
                                        ext = "NSBTP";
                                        break;

                                    case 0x30585442: //BTX0
                                        ext = "NSBTX";
                                        break;

                                    case 0x4E534352: //RCSN
                                        ext = "NSCR";
                                        break;

                                    case 0x4E434552: //RECN
                                        ext = "NCER";
                                        break;

                                    case 0x4E434752: //RGCN
                                        ext = "NCGR";
                                        break;

                                    case 0x4E434C52: //RLCN
                                        ext = "NCLR";
                                        break;

                                    case 0x4E414E52: //RNAN
                                        ext = "NANR";
                                        break;

                                    case 0x4E465452: //RTFN
                                        ext = "NFTR";
                                        break;

                                    case 0x54414453: //SDAT
                                        ext = "SDAT";
                                        break;

                                    case 0x46504353: //SCPF
                                        ext = "SCPF";
                                        break;

                                    default:
                                        ext = "bin";
                                        break;
                                }

                                br.BaseStream.Seek(endereco[i], SeekOrigin.Begin);

                                byte[] data = br.ReadBytes(tamanho[i]);

                                File.WriteAllBytes(Path.Combine(pasta, pasta, i + "." + ext), data);
                            }
                        }
                    }
                }
                MessageBox.Show("Terminado", "AVISO");
            }            
        }

        #endregion

        #region Tenchu Shadow Assassins

        #region Extrator DAT Tenchu Shadow Assassins

        private void button12_Click(object sender, EventArgs e)
        {

            MessageBox.Show("Ainda não implementado");
            /*
            //Extrator arquivo DAT
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo DAT|*.DAT|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo VOLUME.DAT do jogo Tenchu de PlayStation...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        uint magic = Converterendian.bigendian32(br.ReadUInt32());

                        if (magic == 0xFADEBABE)
                        {
                            uint desconhecido = Converterendian.bigendian32(br.ReadUInt32());

                            uint quantidadearquivos = Converterendian.bigendian32(br.ReadUInt32());

                            uint offset = Converterendian.bigendian32(br.ReadUInt32());

                            uint arquivos = Converterendian.bigendian32(br.ReadUInt32());

                            br.BaseStream.Seek(offset, SeekOrigin.Begin);

                            MessageBox.Show("Ainda não implementado");





                        }
                        else
                        {
                            MessageBox.Show("Arquivo inválido!\nInvalid file!", "AVISO! / Warning!");
                            return;
                        }
                    }
                }

                MessageBox.Show("Arquivo Extraido!\nFile extracted!", "AVISO! / Warning!");
            }*/
        }

        #endregion

        #region Insersor DAT Tenchu Shadow Assassins

        private void button13_Click(object sender, EventArgs e)
        {
            //Insersor 
            MessageBox.Show("Ainda não implementado");
        }

        #endregion

        #region Extrator Texto Tenchu Shadow Assassins

        private void button15_Click(object sender, EventArgs e)
        {
            //Extrator Texto
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo BIN|*.BIN|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo BIN do jogo Tenchu de Nintendo Wii ou PSP...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        br.BaseStream.Seek(4, SeekOrigin.Begin);

                        int magic1 = br.ReadInt32();
                        long magic2 = br.ReadInt64();

                        if (magic1 == 0 || magic2 == 0)
                        {
                            br.BaseStream.Seek(0, SeekOrigin.Begin);

                            uint quantidadetextos = Converterendian.bigendian32(br.ReadUInt32());

                            string todosOsTextos = "";

                            for (int i = 0; i < quantidadetextos; i++)
                            {
                                br.BaseStream.Seek((i * 0x10) + 0x10, SeekOrigin.Begin);

                                uint ponteiro = Converterendian.bigendian32(br.ReadUInt32());
                                br.BaseStream.Seek(ponteiro, SeekOrigin.Begin);

                                bool acabouotexto = false;

                                int numerocaracteres = 0;

                                while (acabouotexto == false)
                                {
                                    int comparador;

                                    comparador = br.ReadInt16();

                                    numerocaracteres++;
                                    numerocaracteres++;

                                    if (comparador == 0x00)
                                    {
                                        acabouotexto = true;
                                    }
                                }

                                br.BaseStream.Seek(-numerocaracteres, SeekOrigin.Current);

                                byte[] texto = new byte[numerocaracteres];

                                for (int j = 0; j < numerocaracteres; j++)
                                {
                                    texto[j] = br.ReadByte();
                                }

                                string bigendianunicode = Encoding.BigEndianUnicode.GetString(texto);

                                bigendianunicode = bigendianunicode.Replace("\0", String.Empty);

                                todosOsTextos += bigendianunicode + "<end>\r\n";
                            }

                            File.WriteAllText(Path.Combine(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file)) + ".txt", todosOsTextos);
                        }
                        else
                        {
                            MessageBox.Show("Arquivo inválido!\nInvalid file!", "AVISO! / Warning!");
                            return;
                        }
                    }
                }
                MessageBox.Show("Texto Extraido!\nText extracted!", "AVISO! / Warning!");
            }
        }


        #endregion

        #region Insersor Texto Tenchu Shadow Assassins

        private void button16_Click(object sender, EventArgs e)
        {
            //Insersor Texto
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.Filter = "Arquivo BIN|*.BIN|Todos os arquivos (*.*)|*.*";
            openFileDialog1.Title = "Escolha um arquivo BIN do jogo Tenchu de Nintendo Wii ou PSP...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog1.FileNames)
                {
                    using (FileStream stream = File.Open(file, FileMode.Open))
                    {
                        BinaryReader br = new BinaryReader(stream);
                        BinaryWriter bw = new BinaryWriter(stream);

                        br.BaseStream.Seek(4, SeekOrigin.Begin);

                        int magic1 = br.ReadInt32();
                        long magic2 = br.ReadInt64();

                        if (magic1 == 0 || magic2 == 0)
                        {
                            FileInfo dump = new FileInfo(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".txt");

                            string nomearquivo = Path.GetFileName(file);

                            if (dump.Exists)
                            {
                                /*bw.BaseStream.Seek(offsettexto, SeekOrigin.Begin);

                                for (int i = 0; i < offsetponteiros - offsettexto; i++)
                                {
                                    bw.Write((byte)0);
                                }*/

                                var txt = File.ReadLines(Path.Combine(Path.GetDirectoryName(file), Path.GetFileName(file)) + ".txt");


                            }
                            else
                            {
                                MessageBox.Show(nomearquivo + ".txt file not found!\n\nO arquivo " + nomearquivo + ".txt não foi encontrado!", "Warning! / AVISO!");
                                return;
                            }



                        }
                    }
                }
            }
        }


        #endregion

        #endregion
    }
}
