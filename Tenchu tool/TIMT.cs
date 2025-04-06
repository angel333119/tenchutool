using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Linq;
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
        private int zoomSteps = 0;
        private double zoomFactor = 1.0;

        // ContextMenu para o PictureBox
        private ContextMenuStrip menuDeContexto;

        public TIMT()
        {
            InitializeComponent();

            // Configura os eventos dos ComboBoxes
            comboBoxBinFiles.SelectedIndexChanged += comboBoxBinFiles_SelectedIndexChanged;
            comboBoxImages.SelectedIndexChanged += comboBoxImages_SelectedIndexChanged;

            // Configuração inicial do PictureBox
            pictureBoxDisplay.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxDisplay.Image = null;

            // Configurar o fundo quadriculado e o menu de contexto
            ConfigurarFundoQuadriculado();
            ConfigurarMenuDeContexto();
            pictureBoxDisplay.ContextMenuStrip = menuDeContexto;

            // Configura o evento de scroll para zoom com a roda do mouse
            pictureBoxDisplay.MouseWheel += pictureBoxDisplay_MouseWheel;
            pictureBoxDisplay.Focus();

            // Atualiza o label de zoom inicial
            AtualizarLabelZoom();
        }

        #region Configurações de Interface

        private void ConfigurarFundoQuadriculado()
        {
            // Cria o fundo quadriculado e define no PictureBox
            int tamanhoQuadrado = 10;
            Bitmap fundoQuadriculado = CriarFundoQuadriculado(pictureBoxDisplay.Width, pictureBoxDisplay.Height, tamanhoQuadrado);
            pictureBoxDisplay.BackgroundImage = fundoQuadriculado;
            pictureBoxDisplay.BackgroundImageLayout = ImageLayout.Tile;
        }

        private Bitmap CriarFundoQuadriculado(int largura, int altura, int tamanhoQuadrado)
        {
            Bitmap fundo = new Bitmap(largura, altura);
            using (Graphics g = Graphics.FromImage(fundo))
            {
                Color cor1 = Color.LightGray;
                Color cor2 = Color.White;
                for (int y = 0; y < altura; y += tamanhoQuadrado)
                {
                    for (int x = 0; x < largura; x += tamanhoQuadrado)
                    {
                        bool quadradoPar = ((x / tamanhoQuadrado) + (y / tamanhoQuadrado)) % 2 == 0;
                        using (SolidBrush brush = new SolidBrush(quadradoPar ? cor1 : cor2))
                        {
                            g.FillRectangle(brush, x, y, tamanhoQuadrado, tamanhoQuadrado);
                        }
                    }
                }
            }
            return fundo;
        }

        private void ConfigurarMenuDeContexto()
        {
            menuDeContexto = new ContextMenuStrip();

            var menuExtrairImagem = new ToolStripMenuItem("Extrair imagem como PNG");
            var menuExtrairTodas = new ToolStripMenuItem("Extrair todas as imagens como PNG");
            var menuImportarImagem = new ToolStripMenuItem("Importar imagem PNG");

            menuExtrairImagem.Click += ExtrairImagemComoPng_Click;
            menuExtrairTodas.Click += ExtrairTodasImagensComoPng_Click;
            menuImportarImagem.Click += ImportarImagemPng_Click;

            menuDeContexto.Items.Add(menuExtrairImagem);
            menuDeContexto.Items.Add(menuExtrairTodas);
            menuDeContexto.Items.Add(menuImportarImagem);
        }

        #endregion

        #region Eventos de Arquivos e ComboBoxes

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
                            br.BaseStream.Seek(posHeader + 5, SeekOrigin.Begin);
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private void comboBoxBinFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            PrepararListaDeImagensTIM();
        }

        private void comboBoxImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int indice = comboBoxImages.SelectedIndex;
            ProcessarTIM(indice);
        }

        #endregion

        #region Processamento e Zoom

        // Lê o arquivo selecionado e percorre seu conteúdo em busca de blocos TIM válidos
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
                while (br.BaseStream.Position <= br.BaseStream.Length - 4)
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

                        bool hasClut = (flag & 0x8) != 0;
                        if (hasClut)
                        {
                            int clutBlockSize = br.ReadInt32();
                            br.BaseStream.Seek(clutBlockSize - 4, SeekOrigin.Current);
                        }
                        int imageBlockSize = br.ReadInt32();
                        br.BaseStream.Seek(imageBlockSize - 4, SeekOrigin.Current);
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

        // Processa a textura TIM e converte para um Bitmap usando LockBits para otimização
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
                int bpp = flag & 0x7; // 0 = 4bpp, 1 = 8bpp, 2 = 16bpp, 3 = 24bpp
                bool hasClut = (flag & 0x8) != 0;
                Color[] palette = null;

                if (hasClut)
                {
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

                int imageBlockSize = br.ReadInt32();
                short imageX = br.ReadInt16();
                short imageY = br.ReadInt16();
                short imageWidthWord = br.ReadInt16();
                short imageHeight = br.ReadInt16();
                int imageWidth = (bpp == 0) ? imageWidthWord * 4 :
                                 (bpp == 1) ? imageWidthWord * 2 : imageWidthWord;

                byte[] imageData = br.ReadBytes(imageBlockSize - 12);

                int[] pixelBuffer = new int[imageWidth * imageHeight];
                int dataIndex = 0;

                if (bpp == 0)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        int x = 0;
                        while (x < imageWidth && dataIndex < imageData.Length)
                        {
                            byte valor = imageData[dataIndex++];
                            int indice1 = valor & 0x0F;
                            int indice2 = (valor >> 4) & 0x0F;
                            pixelBuffer[y * imageWidth + x] = (palette != null && indice1 < palette.Length)
                                ? palette[indice1].ToArgb()
                                : Color.Magenta.ToArgb();
                            x++;
                            if (x < imageWidth)
                            {
                                pixelBuffer[y * imageWidth + x] = (palette != null && indice2 < palette.Length)
                                    ? palette[indice2].ToArgb()
                                    : Color.Magenta.ToArgb();
                                x++;
                            }
                        }
                    }
                }
                else if (bpp == 1)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            byte indicePixel = imageData[dataIndex++];
                            pixelBuffer[y * imageWidth + x] = (palette != null && indicePixel < palette.Length)
                                ? palette[indicePixel].ToArgb()
                                : Color.Magenta.ToArgb();
                        }
                    }
                }
                else if (bpp == 2)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            if (dataIndex + 1 < imageData.Length)
                            {
                                ushort pixelData = BitConverter.ToUInt16(imageData, dataIndex);
                                dataIndex += 2;
                                pixelBuffer[y * imageWidth + x] = ConverterCorPSX(pixelData).ToArgb();
                            }
                        }
                    }
                }
                else if (bpp == 3)
                {
                    for (int y = 0; y < imageHeight; y++)
                    {
                        for (int x = 0; x < imageWidth; x++)
                        {
                            if (dataIndex + 2 < imageData.Length)
                            {
                                byte blue = imageData[dataIndex++];
                                byte green = imageData[dataIndex++];
                                byte red = imageData[dataIndex++];
                                pixelBuffer[y * imageWidth + x] = Color.FromArgb(255, red, green, blue).ToArgb();
                            }
                        }
                    }
                }

                imagem = new Bitmap(imageWidth, imageHeight, PixelFormat.Format32bppArgb);
                BitmapData bmpData = imagem.LockBits(new Rectangle(0, 0, imagem.Width, imagem.Height),
                                                       ImageLockMode.WriteOnly,
                                                       imagem.PixelFormat);
                Marshal.Copy(pixelBuffer, 0, bmpData.Scan0, pixelBuffer.Length);
                imagem.UnlockBits(bmpData);

                // Armazena a imagem original para controle de zoom
                originalImage = imagem;
                zoomSteps = 0;
                AtualizarZoomFactor();
                ApplyZoom();
            }
        }

        // Atualiza o fator de zoom baseado nos passos
        private void AtualizarZoomFactor()
        {
            zoomFactor = 1.0 + 0.5 * zoomSteps;
        }

        // Aplica o zoom e centraliza a imagem se necessário
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

            imagemAtual = zoomedImage;
            pictureBoxDisplay.Image = zoomedImage;

            // Define SizeMode: se a imagem for menor que o PictureBox, centraliza; caso contrário, utiliza Zoom
            if (newWidth < pictureBoxDisplay.Width && newHeight < pictureBoxDisplay.Height)
                pictureBoxDisplay.SizeMode = PictureBoxSizeMode.CenterImage;
            else
                pictureBoxDisplay.SizeMode = PictureBoxSizeMode.Zoom;

            pictureBoxDisplay.Refresh();
            AtualizarLabelZoom();
        }

        private void AtualizarLabelZoom()
        {
            int porcentagem = (int)Math.Round(zoomFactor * 100);
            zoomLevel.Text = $"Zoom {porcentagem}%";
        }

        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomSteps++;
            AtualizarZoomFactor();
            ApplyZoom();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            if (zoomSteps <= -1)
            {
                MessageBox.Show("Não é possível diminuir mais o zoom!");
                return;
            }
            zoomSteps--;
            AtualizarZoomFactor();
            ApplyZoom();
        }

        private void pictureBoxDisplay_MouseWheel(object sender, MouseEventArgs e)
        {
            if (Control.ModifierKeys == Keys.Control)
            {
                if (e.Delta > 0)
                    zoomSteps++;
                else
                {
                    if (zoomSteps <= -1)
                    {
                        MessageBox.Show("Não é possível diminuir mais o zoom!");
                        return;
                    }
                    zoomSteps--;
                }
                AtualizarZoomFactor();
                ApplyZoom();
            }
        }

        #endregion

        #region Funcionalidades do Menu de Contexto

        private void ExtrairImagemComoPng_Click(object sender, EventArgs e)
        {
            if (imagemAtual == null)
            {
                MessageBox.Show("Nenhuma imagem para salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int indiceSelecionado = comboBoxImages.SelectedIndex;
            if (indiceSelecionado < 0)
            {
                MessageBox.Show("Selecione uma imagem para salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int endereco = enderecoTIMList[indiceSelecionado];
            string enderecoHex = endereco.ToString("X");

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Imagem PNG|*.png";
                saveFileDialog.Title = "Salvar imagem como PNG";
                saveFileDialog.FileName = $"texture_{enderecoHex}.png";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    imagemAtual.Save(saveFileDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Imagem salva com sucesso.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExtrairTodasImagensComoPng_Click(object sender, EventArgs e)
        {
            if (enderecoTIMList.Count == 0 || string.IsNullOrEmpty(arquivoSelecionado))
            {
                MessageBox.Show("Nenhuma imagem para salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.Description = "Selecione a pasta para salvar as imagens";

                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    string pastaDestino = folderDialog.SelectedPath;
                    string nomeArquivo = Path.GetFileNameWithoutExtension(arquivoSelecionado);
                    string pastaDestinoFinal = Path.Combine(pastaDestino, nomeArquivo);
                    if (!Directory.Exists(pastaDestinoFinal))
                    {
                        Directory.CreateDirectory(pastaDestinoFinal);
                    }

                    for (int i = 0; i < enderecoTIMList.Count; i++)
                    {
                        ProcessarTIM(i);
                        if (imagemAtual != null)
                        {
                            int endereco = enderecoTIMList[i];
                            string enderecoHex = endereco.ToString("X");
                            string caminhoImagem = Path.Combine(pastaDestinoFinal, $"texture_{enderecoHex}.png");
                            imagemAtual.Save(caminhoImagem, ImageFormat.Png);
                        }
                    }

                    MessageBox.Show("Todas as imagens foram salvas com sucesso.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        // Converte a imagem (PNG) para bytes no formato TIM, considerando o bpp.
        // Para bpp 0 e 1, utiliza a paleta fornecida (lista de cores) com busca da cor mais próxima.
        private byte[] ConverterImagemParaTIMBytes(Bitmap novaImagem, int bpp, List<Color> paleta)
        {
            if (bpp == 0) // 4bpp
            {
                int width = novaImagem.Width;
                int height = novaImagem.Height;
                int totalPixels = width * height;
                int totalBytes = (totalPixels + 1) / 2;
                byte[] result = new byte[totalBytes];
                for (int i = 0; i < totalPixels; i++)
                {
                    int x = i % width;
                    int y = i / width;
                    Color c = novaImagem.GetPixel(x, y);
                    int index = (paleta != null) ? FindClosestColorIndex(paleta, c) : 0;
                    if (i % 2 == 0)
                        result[i / 2] = (byte)(index & 0x0F);
                    else
                        result[i / 2] |= (byte)((index & 0x0F) << 4);
                }
                return result;
            }
            else if (bpp == 1) // 8bpp
            {
                int width = novaImagem.Width;
                int height = novaImagem.Height;
                byte[] result = new byte[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color c = novaImagem.GetPixel(x, y);
                        int index = (paleta != null) ? FindClosestColorIndex(paleta, c) : 0;
                        result[y * width + x] = (byte)index;
                    }
                }
                return result;
            }
            else if (bpp == 2) // 16bpp
            {
                int width = novaImagem.Width;
                int height = novaImagem.Height;
                byte[] result = new byte[width * height * 2];
                int idx = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort psx = ConverterCorParaPSX(novaImagem.GetPixel(x, y));
                        result[idx++] = (byte)(psx & 0xFF);
                        result[idx++] = (byte)((psx >> 8) & 0xFF);
                    }
                }
                return result;
            }
            else if (bpp == 3) // 24bpp
            {
                int width = novaImagem.Width;
                int height = novaImagem.Height;
                byte[] result = new byte[width * height * 3];
                int idx = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color c = novaImagem.GetPixel(x, y);
                        result[idx++] = c.B;
                        result[idx++] = c.G;
                        result[idx++] = c.R;
                    }
                }
                return result;
            }
            return null;
        }

        // Gera uma lista de cores (paleta) a partir da imagem, forçando o número exato de cores (maxCores).
        // Se a imagem tiver menos cores, repete a última cor; se tiver mais, utiliza as primeiras.
        private List<Color> GerarPaletaColors(Bitmap imagem, int maxCores)
        {
            HashSet<Color> cores = new HashSet<Color>();
            for (int y = 0; y < imagem.Height; y++)
            {
                for (int x = 0; x < imagem.Width; x++)
                {
                    // Ignora alfa para fins de comparação
                    Color c = Color.FromArgb(255, imagem.GetPixel(x, y).R, imagem.GetPixel(x, y).G, imagem.GetPixel(x, y).B);
                    cores.Add(c);
                }
            }
            List<Color> lista = cores.ToList();
            if (lista.Count < maxCores)
            {
                // Repete a última cor até atingir o número necessário
                Color ultima = lista.Last();
                while (lista.Count < maxCores)
                    lista.Add(ultima);
            }
            else if (lista.Count > maxCores)
            {
                // Seleciona as primeiras maxCores (pode-se implementar quantização melhor)
                lista = lista.Take(maxCores).ToList();
            }
            return lista;
        }

        // Procura o índice da cor mais próxima na paleta
        private int FindClosestColorIndex(List<Color> palette, Color c)
        {
            int bestIndex = 0;
            double bestDist = double.MaxValue;
            for (int i = 0; i < palette.Count; i++)
            {
                Color p = palette[i];
                double dist = Math.Pow(p.R - c.R, 2) + Math.Pow(p.G - c.G, 2) + Math.Pow(p.B - c.B, 2);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        // Converte um valor de 16 bits (15-bit PSX) para um objeto Color
        private Color ConverterCorPSX(ushort corData)
        {
            int r = (corData & 0x1F) << 3;
            int g = ((corData >> 5) & 0x1F) << 3;
            int b = ((corData >> 10) & 0x1F) << 3;
            return Color.FromArgb(255, r, g, b);
        }

        // Converte uma cor (ARGB) para o formato PSX (15-bit)
        private ushort ConverterCorParaPSX(Color cor)
        {
            int r = cor.R >> 3;
            int g = cor.G >> 3;
            int b = cor.B >> 3;
            return (ushort)(r | (g << 5) | (b << 10));
        }

        // Método para sobrescrever a imagem do arquivo TIM com um PNG importado.
        // A imagem importada deve ter as mesmas dimensões e propriedades da imagem original.
        private void ImportarImagemPng_Click(object sender, EventArgs e)
        {
            if (comboBoxImages.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione uma imagem para importar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Imagem PNG|*.png";
                openFileDialog.Title = "Importar imagem PNG";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string caminhoImagem = openFileDialog.FileName;

                    try
                    {
                        Bitmap novaImagem = new Bitmap(caminhoImagem);

                        // Valida as dimensões em relação à imagem original
                        if (originalImage != null && (novaImagem.Width != originalImage.Width || novaImagem.Height != originalImage.Height))
                        {
                            MessageBox.Show("As dimensões da imagem importada não correspondem à original.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Chama o método que sobrescreve a imagem no arquivo TIM
                        SobrescreverImagemNoTIM(novaImagem);

                        // Recarrega a imagem do arquivo TIM para atualizar a interface
                        ProcessarTIM(comboBoxImages.SelectedIndex);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao importar a imagem: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Sobrescreve a imagem (e paleta, se houver) no arquivo TIM com os dados da nova imagem
        private void SobrescreverImagemNoTIM(Bitmap novaImagem)
        {
            int indice = comboBoxImages.SelectedIndex;
            int pos = enderecoTIMList[indice];

            using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.ReadWrite))
            using (BinaryReader br = new BinaryReader(stream))
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                // Posiciona no início do bloco TIM
                stream.Seek(pos, SeekOrigin.Begin);
                int magic = br.ReadInt32(); // Deve ser 0x10
                if (magic != 0x10)
                    throw new Exception("Formato TIM inválido.");

                int flag = br.ReadInt32();
                int bpp = flag & 0x7;
                bool hasClut = (flag & 0x8) != 0;

                int offsetImagem = 0;
                int offsetPaleta = 0;
                int clutBlockSize = 0;
                int numColors = 0;

                if (hasClut)
                {
                    offsetPaleta = (int)stream.Position;
                    clutBlockSize = br.ReadInt32();
                    // Lê os parâmetros da CLUT
                    short clutX = br.ReadInt16();
                    short clutY = br.ReadInt16();
                    short clutWidth = br.ReadInt16();
                    short clutHeight = br.ReadInt16();
                    numColors = clutWidth * clutHeight;
                    // Pula os dados da paleta
                    stream.Seek(clutBlockSize - 12, SeekOrigin.Current);
                }
                offsetImagem = (int)stream.Position;
                int imageBlockSize = br.ReadInt32();
                long posImageHeader = stream.Position;
                short imageX = br.ReadInt16();
                short imageY = br.ReadInt16();
                short imageWidthWord = br.ReadInt16();
                short imageHeight = br.ReadInt16();
                int imageWidth = (bpp == 0) ? imageWidthWord * 4 :
                                 (bpp == 1) ? imageWidthWord * 2 : imageWidthWord;
                int offsetImageData = (int)stream.Position;
                int tamanhoImagemData = imageBlockSize - 12;

                // Valida dimensões
                if (novaImagem.Width != imageWidth || novaImagem.Height != imageHeight)
                {
                    MessageBox.Show("As dimensões da nova imagem não correspondem à original.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                byte[] novaImagemBytes = null;
                byte[] novaPaletaBytes = null;

                if (bpp == 0 || bpp == 1)
                {
                    // Para paletizadas: gera a paleta com o número exato de cores
                    List<Color> paleta = GerarPaletaColors(novaImagem, numColors);
                    novaPaletaBytes = new byte[numColors * 2];
                    for (int i = 0; i < numColors; i++)
                    {
                        ushort psx = ConverterCorParaPSX(paleta[i]);
                        novaPaletaBytes[i * 2] = (byte)(psx & 0xFF);
                        novaPaletaBytes[i * 2 + 1] = (byte)((psx >> 8) & 0xFF);
                    }
                    novaImagemBytes = ConverterImagemParaTIMBytes(novaImagem, bpp, paleta);
                }
                else
                {
                    // Para 16bpp ou 24bpp
                    novaImagemBytes = ConverterImagemParaTIMBytes(novaImagem, bpp, null);
                }

                if (novaImagemBytes.Length != tamanhoImagemData)
                {
                    MessageBox.Show("O tamanho dos dados da imagem convertida não corresponde ao original.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Sobrescreve os dados da imagem
                stream.Seek(offsetImageData, SeekOrigin.Begin);
                bw.Write(novaImagemBytes);

                // Se houver paleta, sobrescreve também
                if (hasClut)
                {
                    long posPaleta = offsetPaleta + 12; // após o cabeçalho da CLUT
                    stream.Seek(posPaleta, SeekOrigin.Begin);
                    int tamanhoPaletaOriginal = clutBlockSize - 12;
                    if (novaPaletaBytes.Length != tamanhoPaletaOriginal)
                    {
                        MessageBox.Show("O tamanho dos dados da paleta convertida não corresponde ao original.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    bw.Write(novaPaletaBytes);
                }
            }
            MessageBox.Show("A imagem foi sobrescrita com sucesso no arquivo TIM.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        #endregion
    }
}
