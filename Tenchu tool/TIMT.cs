using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace Tenchu_tool
{
    public partial class TIMT : Form
    {
        // Listas e mapeamentos para os arquivos e texturas TIM
        private List<int> enderecoTIMList = new List<int>();
        private string arquivoSelecionado;
        private Dictionary<string, string> arquivosTimMap = new Dictionary<string, string>();

        // Imagem atual processada, imagem original e fator de zoom
        private Bitmap imagemAtual;
        private Bitmap originalImage;
        private double zoomFactor = 1.0;

        public TIMT()
        {
            InitializeComponent();

            // Configura os eventos dos ComboBoxes
            comboBoxBinFiles.SelectedIndexChanged += comboBoxBinFiles_SelectedIndexChanged;
            comboBoxImages.SelectedIndexChanged += comboBoxImages_SelectedIndexChanged;

            // Configuração inicial
            pictureBoxDisplay.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxDisplay.Image = null;
        }

        // Evento do botão para abrir arquivos TIM ou ARC
        private void buttonAbrirArquivos_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Arquivos Tenchu|*.tim;*.ARC|Todos os Arquivos|*.*";
            openFileDialog.Title = "Selecione o contêiner TIM";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                ReceberArquivosTim(openFileDialog.FileNames);
            }
        }

        // Mapeia os arquivos que contêm dados TIM
        private void ReceberArquivosTim(string[] arquivos)
        {
            comboBoxBinFiles.Items.Clear();
            arquivosTimMap.Clear();
            comboBoxImages.Items.Clear();

            foreach (var arquivo in arquivos)
            {
                // Verifica se o arquivo contém dados TIM realizando uma busca completa no arquivo
                if (ArquivoContemTIM(arquivo))
                {
                    string nomeArquivo = Path.GetFileName(arquivo);
                    comboBoxBinFiles.Items.Add(nomeArquivo);
                    arquivosTimMap[nomeArquivo] = arquivo;
                }
            }

            if (comboBoxBinFiles.Items.Count > 0)
                comboBoxBinFiles.Text = "Selecione o arquivo TIM aqui";
            else
                comboBoxBinFiles.Text = "Nenhum arquivo TIM encontrado.";
        }

        // Verifica se o arquivo contém pelo menos um TIM válido, buscando em todo o arquivo
        private bool ArquivoContemTIM(string caminhoArquivo)
        {
            try
            {
                using (FileStream stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    while (br.BaseStream.Position < br.BaseStream.Length)
                    {
                        byte b = br.ReadByte();
                        if (b == 0x10) // Possível início de TIM
                        {
                            long posHeader = br.BaseStream.Position - 1;
                            br.BaseStream.Seek(posHeader, SeekOrigin.Begin);

                            int possibleHeader = br.ReadInt32();
                            if (possibleHeader == 0x10)
                            {
                                int flag = br.ReadInt32();
                                int bpp = flag & 0x7;
                                if (bpp >= 0 && bpp <= 3)
                                    return true;
                            }
                            // Avança 1 byte além dos 4 já lidos
                            br.BaseStream.Seek(posHeader + 5, SeekOrigin.Begin);
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        // Quando o usuário seleciona um arquivo no comboBoxBinFiles
        private void comboBoxBinFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            PrepararListaDeImagensTIM();
        }

        // Lê o arquivo selecionado e percorre todo o seu conteúdo para procurar blocos TIM válidos
        private void PrepararListaDeImagensTIM()
        {
            if (comboBoxBinFiles.SelectedItem == null)
                return;
            string nomeSelecionado = comboBoxBinFiles.SelectedItem.ToString();
            if (!arquivosTimMap.TryGetValue(nomeSelecionado, out arquivoSelecionado))
                return;

            enderecoTIMList.Clear();
            comboBoxImages.Items.Clear();
            imagemAtual = null;
            pictureBoxDisplay.Image = null;

            using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(stream))
            {
                // Percorre todo o arquivo
                while (br.BaseStream.Position < br.BaseStream.Length - 4)
                {
                    long pos = br.BaseStream.Position;
                    int header = br.ReadInt32();
                    if (header == 0x10)
                    {
                        int flag = br.ReadInt32();
                        int bpp = flag & 0x7; // valores válidos: 0, 1, 2 ou 3
                        if (bpp < 0 || bpp > 3)
                        {
                            br.BaseStream.Seek(pos + 1, SeekOrigin.Begin);
                            continue;
                        }
                        enderecoTIMList.Add((int)pos);
                        Console.WriteLine($"TIM encontrado em {pos:X} com flag: 0x{flag:X}");

                        bool hasClut = (flag & 0x8) != 0;
                        if (hasClut)
                        {
                            int clutBlockSize = br.ReadInt32();
                            br.BaseStream.Seek(clutBlockSize, SeekOrigin.Current);
                        }
                        int imageBlockSize = br.ReadInt32();
                        br.BaseStream.Seek(imageBlockSize, SeekOrigin.Current);
                    }
                    else
                    {
                        br.BaseStream.Seek(pos + 1, SeekOrigin.Begin);
                    }
                }
            }

            // Preenche o comboBoxImages com as texturas encontradas
            comboBoxImages.Enabled = (enderecoTIMList.Count > 0);
            for (int i = 0; i < enderecoTIMList.Count; i++)
            {
                comboBoxImages.Items.Add("Texture " + (i + 1));
            }
            if (enderecoTIMList.Count > 0)
            {
                comboBoxImages.SelectedIndex = 0;
                ProcessarTIM(0);
            }
        }

        // Quando o usuário seleciona uma textura no comboBoxImages
        private void comboBoxImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int indice = comboBoxImages.SelectedIndex;
            ProcessarTIM(indice);
        }

        // Processa a textura TIM e converte para um Bitmap
        private void ProcessarTIM(int indice)
        {
            if (indice < 0 || indice >= enderecoTIMList.Count)
                return;

            Bitmap imagem = null;

            using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(stream))
            {
                br.BaseStream.Seek(enderecoTIMList[indice], SeekOrigin.Begin);
                int magic = br.ReadInt32(); // Deve ser 0x10
                if (magic != 0x10)
                    return;

                int flag = br.ReadInt32();
                int bpp = flag & 7; // 0 = 4bpp, 1 = 8bpp, 2 = 16bpp, 3 = 24bpp
                bool hasClut = (flag & 0x8) != 0;
                Color[] palette = null;

                if (hasClut)
                {
                    // Lê o bloco da CLUT
                    int clutBlockSize = br.ReadInt32();
                    short clutX = br.ReadInt16();
                    short clutY = br.ReadInt16();
                    short clutWidth = br.ReadInt16();
                    short clutHeight = br.ReadInt16();
                    int numColors = clutWidth * clutHeight;
                    palette = new Color[numColors];
                    for (int i = 0; i < numColors; i++)
                    {
                        ushort corData = br.ReadUInt16();
                        palette[i] = ConverterCorPSX(corData);
                    }
                }

                // Lê o bloco de imagem
                int imageBlockSize = br.ReadInt32();
                short imageX = br.ReadInt16();
                short imageY = br.ReadInt16();
                short imageWidthWord = br.ReadInt16(); // largura em "words" (depende do bpp)
                short imageHeight = br.ReadInt16();
                int imageWidth = 0;

                // Converte a largura para pixels conforme o bpp
                if (bpp == 0)      // 4bpp: cada word (2 bytes) contém 4 pixels
                    imageWidth = imageWidthWord * 4;
                else if (bpp == 1) // 8bpp: cada word contém 2 pixels
                    imageWidth = imageWidthWord * 2;
                else if (bpp == 2) // 16bpp: cada word é 1 pixel
                    imageWidth = imageWidthWord;
                else if (bpp == 3) // 24bpp: assumimos que imageWidthWord já representa o número de pixels
                    imageWidth = imageWidthWord;

                byte[] imageData = br.ReadBytes(imageBlockSize - 12);

                // Cria o Bitmap com as dimensões determinadas
                imagem = new Bitmap(imageWidth, imageHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                if (bpp == 0 || bpp == 1)
                {
                    // Imagem paletizada
                    int dataIndex = 0;
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth;)
                        {
                            if (bpp == 0)
                            {
                                // 4bpp: cada byte contém dois pixels
                                byte valor = imageData[dataIndex++];
                                int indice1 = valor & 0x0F;
                                int indice2 = (valor >> 4) & 0x0F;
                                if (x < imageWidth)
                                {
                                    Color c1 = (palette != null && indice1 < palette.Length) ? palette[indice1] : Color.Magenta;
                                    imagem.SetPixel(x, y, c1);
                                    x++;
                                }
                                if (x < imageWidth)
                                {
                                    Color c2 = (palette != null && indice2 < palette.Length) ? palette[indice2] : Color.Magenta;
                                    imagem.SetPixel(x, y, c2);
                                    x++;
                                }
                            }
                            else if (bpp == 1)
                            {
                                // 8bpp: cada byte é um índice direto na paleta
                                byte indicePixel = imageData[dataIndex++];
                                Color c = (palette != null && indicePixel < palette.Length) ? palette[indicePixel] : Color.Magenta;
                                imagem.SetPixel(x, y, c);
                                x++;
                            }
                        }
                    }
                }
                else if (bpp == 2)
                {
                    // 16bpp: cada pixel ocupa 2 bytes (cor direta no formato PSX)
                    int dataIndex = 0;
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            if (dataIndex + 1 < imageData.Length)
                            {
                                ushort pixelData = BitConverter.ToUInt16(imageData, dataIndex);
                                dataIndex += 2;
                                Color c = ConverterCorPSX(pixelData);
                                imagem.SetPixel(x, y, c);
                            }
                        }
                    }
                }
                else if (bpp == 3)
                {
                    // 24bpp: cada pixel ocupa 3 bytes (ordem: Blue, Green, Red)
                    int dataIndex = 0;
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            if (dataIndex + 2 < imageData.Length)
                            {
                                byte blue = imageData[dataIndex++];
                                byte green = imageData[dataIndex++];
                                byte red = imageData[dataIndex++];
                                Color c = Color.FromArgb(255, red, green, blue);
                                imagem.SetPixel(x, y, c);
                            }
                        }
                    }
                }
            }

            // Armazena a imagem original para controle de zoom
            originalImage = imagem;
            // Reseta o fator de zoom para 1.0 (tamanho original)
            zoomFactor = 1.0;
            ApplyZoom();
        }

        // Aplica o zoom com base no zoomFactor
        private void ApplyZoom()
        {
            if (originalImage == null)
                return;

            int newWidth = (int)(originalImage.Width * zoomFactor);
            int newHeight = (int)(originalImage.Height * zoomFactor);

            Bitmap zoomedImage = new Bitmap(newWidth, newHeight);
            using (Graphics g = Graphics.FromImage(zoomedImage))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(originalImage, 0, 0, newWidth, newHeight);
            }

            pictureBoxDisplay.Image = zoomedImage;
            pictureBoxDisplay.SizeMode = PictureBoxSizeMode.Normal;
            pictureBoxDisplay.Refresh();
        }

        // Evento do botão Zoom In (aumenta o zoom em 50%)
        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomFactor *= 1.5;
            ApplyZoom();
        }

        // Evento do botão Zoom Out (diminui o zoom em 50%)
        // Permite diminuir apenas uma vez quando o zoom estiver em 100%
        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            // Se o zoom já estiver abaixo de 1.0, impede nova diminuição
            if (zoomFactor < 1.0)
            {
                MessageBox.Show("Não é possível diminuir mais o zoom!");
                return;
            }
            zoomFactor *= 0.5;
            ApplyZoom();
        }

        // Converte um valor de 16 bits (15-bit PSX) para um objeto Color
        private Color ConverterCorPSX(ushort corData)
        {
            int r = (corData & 0x1F) << 3;
            int g = ((corData >> 5) & 0x1F) << 3;
            int b = ((corData >> 10) & 0x1F) << 3;
            int a = (corData & 0x8000) != 0 ? 0 : 255;
            return Color.FromArgb(a, r, g, b);
        }
    }
}
