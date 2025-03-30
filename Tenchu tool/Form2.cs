using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tenchu_tool
{
    public partial class Form2 : Form
    {
        private List<int> enderecoK2TxList = new List<int>();
        private string arquivoSelecionado;
        private Dictionary<string, string> arquivosBinMap = new Dictionary<string, string>();
        private List<Bitmap> imagensExtraidas = new List<Bitmap>();
        private Bitmap imagemAtual;
        private ContextMenuStrip menuDeContexto;
        public int magic;

        public Form2()
        {
            InitializeComponent();

            // Configurar o modo do PictureBox
            pictureBoxDisplay.SizeMode = PictureBoxSizeMode.Zoom;

            // Define o fundo quadriculado
            int tamanhoQuadrado = 10; // Tamanho de cada quadrado do padrão
            Bitmap fundoQuadriculado = CriarFundoQuadriculado(pictureBoxDisplay.Width, pictureBoxDisplay.Height, tamanhoQuadrado);
            pictureBoxDisplay.BackgroundImage = fundoQuadriculado;
            pictureBoxDisplay.BackgroundImageLayout = ImageLayout.Tile;

            // Conectar eventos
            comboBoxBinFiles.SelectedIndexChanged += comboBoxBinFiles_SelectedIndexChanged;
            comboBoxImages.SelectedIndexChanged += comboBoxImages_SelectedIndexChanged;

            // Estado inicial da interface
            pictureBoxDisplay.Image = null;
            comboBoxImages.Items.Clear();

            // Configurar o menu de contexto
            ConfigurarMenuDeContexto();
            pictureBoxDisplay.ContextMenuStrip = menuDeContexto; // Vincula o menu ao PictureBox

            //Permite arrastar e soltar os arquivos
            pictureBoxDisplay.AllowDrop = true;
            pictureBoxDisplay.DragEnter += pictureBox_DragEnter;
            pictureBoxDisplay.DragDrop += pictureBox_DragDrop;
        }

        private void ConfigurarMenuDeContexto()
        {
            menuDeContexto = new ContextMenuStrip();

            var menuExtrairImagem = new ToolStripMenuItem("Extrair imagem como PNG");
            var menuExtrairTodas = new ToolStripMenuItem("Extrair todas as imagens como PNG");
            var menuImportarImagem = new ToolStripMenuItem("Importar imagem PNG"); // Nova opção

            menuExtrairImagem.Click += ExtrairImagemComoPng_Click;
            menuExtrairTodas.Click += ExtrairTodasImagensComoPng_Click;
            menuImportarImagem.Click += ImportarImagemPng_Click; // Evento para importar imagem

            menuDeContexto.Items.Add(menuExtrairImagem);
            menuDeContexto.Items.Add(menuExtrairTodas);
            menuDeContexto.Items.Add(menuImportarImagem); // Adiciona ao menu
        }

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

                        // Verifica se as dimensões da imagem são compatíveis
                        if (novaImagem.Width != pictureBoxDisplay.Image.Width || novaImagem.Height != pictureBoxDisplay.Image.Height)
                        {
                            MessageBox.Show("As dimensões da imagem não correspondem às do arquivo original.", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Substituir a imagem no arquivo BIN
                        try
                        {
                            byte[] novaPaleta = GerarPaleta(novaImagem);
                            SubstituirImagemNoBin(comboBoxImages.SelectedIndex, novaImagem, novaPaleta);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erro ao importar a imagem: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao importar a imagem: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SubstituirImagemNoBin(int indice, Bitmap novaImagem, byte[] novaPaleta)
        {
            if (indice < 0 || indice >= enderecoK2TxList.Count) return;

            using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(stream))
            using (BinaryWriter bw = new BinaryWriter(stream))
            {
                // Vai para o offset correspondente da imagem no arquivo BIN
                stream.Seek(enderecoK2TxList[indice], SeekOrigin.Begin);

                // Lê os dados de cabeçalho da imagem original
                magic = br.ReadInt32();
                int offsetdaimagem = br.ReadInt32();
                int offsetdapaleta = br.ReadInt32();
                int tamanhoimagem = br.ReadInt32();
                ushort larguraOriginal = br.ReadUInt16();
                ushort alturaOriginal = br.ReadUInt16();

                // Verifica se a nova imagem tem o mesmo tamanho que a original
                if (novaImagem.Width != larguraOriginal || novaImagem.Height != alturaOriginal)
                {
                    MessageBox.Show(
                        $"A nova imagem deve ter as dimensões {larguraOriginal}x{alturaOriginal}.",
                        "Erro de Dimensão",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                // Calcula o tamanho esperado da paleta (256 entradas de 4 bytes cada)
                int tamanhoPaletaOriginal = tamanhoimagem - offsetdapaleta;

                // Verifica se a nova paleta tem o mesmo tamanho que a original
                if (novaPaleta.Length != tamanhoPaletaOriginal)
                {
                    MessageBox.Show(
                        $"A nova paleta deve ter exatamente {tamanhoPaletaOriginal} bytes.",
                        "Erro de Tamanho da Paleta",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }

                // Determinar se a imagem e a paleta precisam ser swizzled ou não
                bool precisaSwizzle = (offsetdaimagem == 0x80);

                // Converte o bitmap e a paleta para bytes no formato correto (swizzled ou unswizzled)
                byte[] imagemBytes = precisaSwizzle
                    ? AplicarSwizzle(novaImagem, larguraOriginal, alturaOriginal)
                    : BitmapParaBytes(novaImagem, larguraOriginal, alturaOriginal);

                byte[] paletaBytes = precisaSwizzle
                    ? AplicarSwizzlePaleta(novaPaleta)
                    : novaPaleta;

                // Substituir os bytes da imagem
                bw.BaseStream.Seek(enderecoK2TxList[indice] + offsetdaimagem, SeekOrigin.Begin);
                bw.Write(imagemBytes);

                // Substituir os bytes da paleta
                bw.BaseStream.Seek(enderecoK2TxList[indice] + offsetdapaleta, SeekOrigin.Begin);
                bw.Write(paletaBytes);
            }

            MessageBox.Show(
                "A imagem e a paleta foram substituídas com sucesso no arquivo BIN.",
                "Sucesso",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private byte[] AplicarSwizzle(Bitmap imagem, int largura, int altura)
        {
            byte[] swizzled = new byte[largura * altura];
            int[] pixelData = new int[largura * altura];
            int index = 0;

            // Extrair os pixels do bitmap
            for (int y = 0; y < altura; y++)
            {
                for (int x = 0; x < largura; x++)
                {
                    Color pixel = imagem.GetPixel(x, y);
                    pixelData[index++] = pixel.ToArgb();
                }
            }

            // Aplicar o algoritmo de swizzling
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

                    if (swizzleid < pixelData.Length)
                    {
                        swizzled[swizzleid] = (byte)pixelData[y * largura + x];
                    }
                }
            }

            return swizzled;
        }

        private byte[] GerarPaleta(Bitmap imagem)
        {
            // Assume que a imagem original usa uma paleta com até 256 cores.
            HashSet<Color> cores = new HashSet<Color>();

            for (int y = 0; y < imagem.Height; y++)
            {
                for (int x = 0; x < imagem.Width; x++)
                {
                    cores.Add(imagem.GetPixel(x, y));
                    if (cores.Count > 256) break; // Limite de 256 cores
                }
                if (cores.Count > 256) break;
            }

            if (cores.Count > 256)
            {
                throw new Exception("A imagem importada possui mais de 256 cores.");
            }

            byte[] paleta = new byte[1024]; // 256 cores * 4 bytes (RGBA)

            int index = 0;
            foreach (var cor in cores)
            {
                paleta[index++] = cor.R;
                paleta[index++] = cor.G;
                paleta[index++] = cor.B;
                paleta[index++] = cor.A;
            }

            return paleta;
        }

        private byte[] AplicarSwizzlePaleta(byte[] paleta)
        {
            byte[] swizzledPaleta = new byte[paleta.Length];

            for (int p = 0; p < 256; p++)
            {
                int pos = ((p & 231) + ((p & 8) << 1) + ((p & 16) >> 1)) * 4;
                if (pos + 4 <= swizzledPaleta.Length)
                {
                    Buffer.BlockCopy(paleta, p * 4, swizzledPaleta, pos, 4);
                }
            }

            return swizzledPaleta;
        }

        private byte[] BitmapParaBytes(Bitmap imagem, int largura, int altura)
        {
            List<byte> imagemBytes = new List<byte>();

            for (int y = 0; y < altura; y++)
            {
                for (int x = 0; x < largura; x++)
                {
                    Color pixel = imagem.GetPixel(x, y);

                    // Aqui você precisa mapear o pixel para o índice da paleta original.
                    // Para simplificação, assumimos que o arquivo BIN aceita RGBA direto.
                    imagemBytes.Add(pixel.R);
                    imagemBytes.Add(pixel.G);
                    imagemBytes.Add(pixel.B);
                    imagemBytes.Add(pixel.A);
                }
            }

            return imagemBytes.ToArray();
        }

        private void ExtrairImagemComoPng_Click(object sender, EventArgs e)
        {
            if (imagemAtual == null)
            {
                MessageBox.Show("Nenhuma imagem para salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Obtém o índice da imagem selecionada
            int indiceSelecionado = comboBoxImages.SelectedIndex;
            if (indiceSelecionado < 0)
            {
                MessageBox.Show("Selecione uma imagem para salvar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Converte o endereço para hexadecimal
            int endereco = enderecoK2TxList[indiceSelecionado];
            string enderecoHex = endereco.ToString("X");

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Imagem PNG|*.png";
                saveFileDialog.Title = "Salvar imagem como PNG";
                saveFileDialog.FileName = $"image_{enderecoHex}"; // Nome definido com o endereço em hexadecimal

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    imagemAtual.Save(saveFileDialog.FileName, ImageFormat.Png);
                    MessageBox.Show("Imagem salva com sucesso.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ExtrairTodasImagensComoPng_Click(object sender, EventArgs e)
        {
            if (enderecoK2TxList.Count == 0 || string.IsNullOrEmpty(arquivoSelecionado))
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

                    // Cria uma subpasta com o nome do arquivo BIN (sem a extensão)
                    string nomeArquivo = Path.GetFileNameWithoutExtension(arquivoSelecionado);
                    string pastaDestinoFinal = Path.Combine(pastaDestino, nomeArquivo);
                    if (!Directory.Exists(pastaDestinoFinal))
                    {
                        Directory.CreateDirectory(pastaDestinoFinal);
                    }

                    // Processar e salvar cada imagem
                    for (int i = 0; i < enderecoK2TxList.Count; i++)
                    {
                        Bitmap imagem = ProcessarImagemParaSalvar(i); // Processa a imagem
                        if (imagem != null)
                        {
                            // Converte o endereço para hexadecimal (com 5 dígitos, se desejar)
                            int endereco = enderecoK2TxList[i];
                            string enderecoHex = endereco.ToString("X5");

                            string caminhoImagem = Path.Combine(pastaDestinoFinal, $"image_{enderecoHex}.png");
                            imagem.Save(caminhoImagem, ImageFormat.Png);
                        }
                    }

                    MessageBox.Show("Todas as imagens foram salvas com sucesso.", "Informação", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private Bitmap ProcessarImagemParaSalvar(int indice)
        {
            return ProcessarImagemInterna(indice);
        }

        public void ReceberArquivosBin(string[] arquivos)
        {
            comboBoxBinFiles.Items.Clear();
            arquivosBinMap.Clear(); // Limpar o mapeamento antigo
            comboBoxImages.Items.Clear(); // Limpa o ComboBox de imagens

            foreach (var arquivo in arquivos)
            {
                string nomeArquivo = Path.GetFileName(arquivo); // Obtém apenas o nome do arquivo

                // Verifica se o arquivo contém texturas
                if (ArquivoContemTexturas(arquivo))
                {
                    comboBoxBinFiles.Items.Add(nomeArquivo); // Adiciona o nome ao ComboBox
                    arquivosBinMap[nomeArquivo] = arquivo;  // Mapeia o nome ao caminho completo
                }
            }

            // Define mensagem após abrir arquivos
            if (comboBoxBinFiles.Items.Count > 0)
            {
                comboBoxImages.Enabled = false; // Desativa o ComboBox antes de preenchê-lo
                comboBoxBinFiles.Text = "Select the BIN file here"; // Mensagem após abrir arquivos
            }
            else
            {
                comboBoxBinFiles.Text = "No BIN files with textures found."; // Mensagem caso nenhum arquivo válido seja encontrado
            }
        }

        private bool ArquivoContemTexturas(string caminhoArquivo)
        {
            try
            {
                using (FileStream stream = File.Open(caminhoArquivo, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long tamanhoArquivo = stream.Length;

                    // Procura por texturas no arquivo (identificador 'K2Tx')
                    for (int a = 0; a < tamanhoArquivo / 4 - 200; a++)
                    {
                        int K2Tx = br.ReadInt32();
                        if (K2Tx == 0x7854324B || K2Tx == 0x20703274) // 'K2Tx' em hexadecimal
                        {
                            return true; // Contém texturas
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao verificar texturas no arquivo: {ex.Message}");
            }

            return false; // Não contém texturas ou ocorreu um erro
        }

        private async void PrepararListaDeImagens()
        {
            if (comboBoxBinFiles.SelectedItem == null || comboBoxBinFiles.Text == "Select a BIN file here") return;

            string nomeSelecionado = comboBoxBinFiles.SelectedItem.ToString();
            if (!arquivosBinMap.TryGetValue(nomeSelecionado, out arquivoSelecionado)) return;

            enderecoK2TxList.Clear();
            comboBoxImages.Items.Clear();
            comboBoxImages.Enabled = false;
            pictureBoxDisplay.Image = null;

            await Task.Run(() =>
            {
                using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (BinaryReader br = new BinaryReader(stream))
                {
                    long tamanhoArquivo = stream.Length;

                    for (int a = 0; a < tamanhoArquivo / 4 - 200; a++)
                    {
                        int K2Tx_ou_T2P = br.ReadInt32();
                        if (K2Tx_ou_T2P == 0x7854324B || K2Tx_ou_T2P == 0x20703274)
                        {
                            enderecoK2TxList.Add(a * 4);
                        }
                    }
                }
            });

            for (int i = 0; i < enderecoK2TxList.Count; i++)
            {
                comboBoxImages.Items.Add($"Image {i + 1}");
            }

            comboBoxImages.Enabled = enderecoK2TxList.Count > 0;

            if (enderecoK2TxList.Count > 0)
            {
                ProcessarImagem(0);
                comboBoxImages.SelectedIndex = 0;
            }
        }

        private Bitmap ProcessarImagemInterna(int indice)
        {
            if (indice < 0 || indice >= enderecoK2TxList.Count)
                return null;

            Bitmap imagem = null;

            using (FileStream stream = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BinaryReader br = new BinaryReader(stream))
            {
                br.BaseStream.Seek(enderecoK2TxList[indice], SeekOrigin.Begin);
                magic = br.ReadInt32();

                if (magic == 0x7854324B) // Se for K2tx
                {
                    int offsetdaimagem = br.ReadInt32();
                    int offsetdapaleta = br.ReadInt32();
                    int tamanhoimagem = br.ReadInt32();
                    ushort largura = br.ReadUInt16();
                    ushort altura = br.ReadUInt16();

                    br.BaseStream.Seek(offsetdaimagem + enderecoK2TxList[indice], SeekOrigin.Begin);
                    byte[] imagemBytes = br.ReadBytes(tamanhoimagem - offsetdaimagem);

                    br.BaseStream.Seek(offsetdapaleta + enderecoK2TxList[indice], SeekOrigin.Begin);
                    byte[] paletaBytes = br.ReadBytes(tamanhoimagem - offsetdapaleta);

                    // Verificar se é necessário aplicar unswizzle
                    if (offsetdaimagem == 0x80)
                    {
                        br.BaseStream.Seek(enderecoK2TxList[indice] + 0x20, SeekOrigin.Begin);
                        int verificador1 = br.ReadInt32();
                        int verificador2 = br.ReadInt32();

                        if ((verificador1 == 33554432 && verificador2 == 512) ||
                            (verificador1 == 16777216 && verificador2 == 256) ||
                            (verificador1 == 8388608 && verificador2 == 128) ||
                            (verificador1 == 4194304 && verificador2 == 64) ||
                            (verificador1 == 33554432 && verificador2 == 128))
                        {
                            imagemBytes = AplicarUnSwizzle(imagemBytes, largura, altura);
                        }
                    }

                    imagem = CriarBitmapk2tx(largura, altura, imagemBytes, paletaBytes);
                }
                else if (magic == 0x20703274) // Se for T2p
                {
                    int offsetdaimagem = br.ReadInt32();
                    int offsetdapaleta = br.ReadInt32();
                    int tamanhoimagem = br.ReadInt32();
                    ushort largura = br.ReadUInt16();
                    ushort altura = br.ReadUInt16();

                    br.BaseStream.Seek(offsetdaimagem + enderecoK2TxList[indice], SeekOrigin.Begin);
                    byte[] imagemBytes = br.ReadBytes(tamanhoimagem - offsetdaimagem);

                    br.BaseStream.Seek(offsetdapaleta + enderecoK2TxList[indice], SeekOrigin.Begin);
                    byte[] paletaBytes = br.ReadBytes(tamanhoimagem - offsetdapaleta);

                    imagem = CriarBitmapP2t(largura, altura, imagemBytes, paletaBytes);
                }
            }

            return imagem;
        }

        private async void ProcessarImagem(int indice)
        {
            Bitmap imagem = null;

            await Task.Run(() =>
            {
                imagem = ProcessarImagemInterna(indice);
            });

            if (imagem != null)
            {
                imagemAtual = imagem;
                imagensExtraidas.Add(imagem);
                pictureBoxDisplay.Image = imagem;
                pictureBoxDisplay.SizeMode = (imagem.Width > pictureBoxDisplay.Width || imagem.Height > pictureBoxDisplay.Height)
                                                ? PictureBoxSizeMode.Zoom
                                                : PictureBoxSizeMode.CenterImage;
                pictureBoxDisplay.Refresh();
            }

        }

        private byte[] AplicarUnSwizzle(byte[] imagemBytes, int largura, int altura)
        {
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

                    if (swizzleid < imagemBytes.Length)
                    {
                        unswizzled[y * largura + x] = imagemBytes[swizzleid];
                    }
                }
            }
            return unswizzled;
        }

        private Bitmap CriarBitmapk2tx(int largura, int altura, byte[] imagemBytes, byte[] paletaBytes)
        {
            Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);

            // Des-swizzling da paleta (ajuste de ordem das cores)
            byte[] unswizzledPalette = new byte[1024];
            for (int p = 0; p < 256; p++)
            {
                int pos = ((p & 231) + ((p & 8) << 1) + ((p & 16) >> 1));
                if (p * 4 + 4 <= paletaBytes.Length && pos * 4 + 4 <= unswizzledPalette.Length)
                {
                    Buffer.BlockCopy(paletaBytes, p * 4, unswizzledPalette, pos * 4, 4);
                }
            }

            // Preenchendo o Bitmap com base nos índices da imagem e paleta
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
                            byte adjustedAlpha = (byte)Math.Min(a * 2, 255);

                            Color color = Color.FromArgb(adjustedAlpha, r, g, b);
                            bitmap.SetPixel(x, y, color);
                        }
                    }
                }
            }

            return bitmap;
        }

        private Bitmap CriarBitmapP2t(int largura, int altura, byte[] imagemBytes, byte[] paletaBytes)
        {
            // Define as dimensões do tile (16x8 pixels)
            int tileWidth = 16;
            int tileHeight = 8;
            int tileSize = tileWidth * tileHeight; // 128 bytes por tile

            // Calcula quantos tiles cabem horizontalmente e verticalmente
            int tilesX = largura / tileWidth;   // Exemplo: 256 / 16 = 16
            int tilesY = altura / tileHeight;     // Exemplo: 128 / 8 = 16

            Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);

            // Percorre cada tile da imagem
            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    // Calcula o índice base do tile no array de imagem
                    int tileIndex = (tileY * tilesX + tileX);
                    int baseIndex = tileIndex * tileSize;

                    // Percorre cada pixel dentro do tile (em ordem linha a linha)
                    for (int y = 0; y < tileHeight; y++)
                    {
                        for (int x = 0; x < tileWidth; x++)
                        {
                            int dataIndex = baseIndex + (y * tileWidth + x);

                            if (dataIndex < imagemBytes.Length)
                            {
                                byte colorIndex = imagemBytes[dataIndex];
                                int paletteOffset = colorIndex * 4;

                                if (paletteOffset + 3 < paletaBytes.Length)
                                {
                                    byte r = paletaBytes[paletteOffset];
                                    byte g = paletaBytes[paletteOffset + 1];
                                    byte b = paletaBytes[paletteOffset + 2];
                                    byte a = paletaBytes[paletteOffset + 3];

                                    // Calcula a posição final do pixel na imagem
                                    int pixelX = tileX * tileWidth + x;
                                    int pixelY = tileY * tileHeight + y;

                                    bitmap.SetPixel(pixelX, pixelY, Color.FromArgb(a, r, g, b));
                                }
                            }
                        }
                    }
                }
            }

            return bitmap;
        }

        private void comboBoxBinFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBoxBinFiles.SelectedIndex >= 0 && comboBoxBinFiles.Text != "Select the BIN file here")
            {
                PrepararListaDeImagens();
            }
        }

        private void comboBoxImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            int indice = comboBoxImages.SelectedIndex;
            ProcessarImagem(indice);
        }

        private void buttonAbrirArquivos_Click(object sender, EventArgs e)
        {
            enderecoK2TxList.Clear();
            comboBoxImages.Items.Clear();
            pictureBoxDisplay.Image = null;

            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Arquivo Tenchu|*.bin|All files (*.*)|*.*";
            openFileDialog1.Title = "Select a Tenchu File...";
            openFileDialog1.Multiselect = true;

            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ReceberArquivosBin(openFileDialog1.FileNames); // Passa os arquivos para o método existente
            }
        }

        private void pictureBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void pictureBox_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                ReceberArquivosBin(files);
            }
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
    }
}