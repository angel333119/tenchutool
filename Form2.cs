﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
            int tamanhoQuadrado = 10;
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
            pictureBoxDisplay.ContextMenuStrip = menuDeContexto;

            // Permite arrastar e soltar os arquivos
            pictureBoxDisplay.AllowDrop = true;
            pictureBoxDisplay.DragEnter += pictureBox_DragEnter;
            pictureBoxDisplay.DragDrop += pictureBox_DragDrop;
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

        private void ImportarImagemPng_Click(object sender, EventArgs e)
        {
            if (comboBoxImages.SelectedIndex < 0)
            {
                MessageBox.Show("Selecione uma imagem para importar.", "Aviso",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Imagem PNG|*.png";
                ofd.Title = "Importar imagem PNG";

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    Bitmap novaImagem = new Bitmap(ofd.FileName);
                    if (imagemAtual == null ||
                        novaImagem.Width != imagemAtual.Width ||
                        novaImagem.Height != imagemAtual.Height)
                    {
                        MessageBox.Show(
                            $"A nova imagem deve ter as dimensões {imagemAtual.Width}x{imagemAtual.Height}.",
                            "Erro de Dimensão", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    SubstituirImagemNoBin(comboBoxImages.SelectedIndex, novaImagem);
                }
            }
        }
        private void SubstituirImagemNoBin(int indice, Bitmap novaImagem)
        {
            if (indice < 0 || indice >= enderecoK2TxList.Count) return;

            try
            {
                int magic, offImg, offPal;
                ushort w, h;
                long baseOffset = enderecoK2TxList[indice];

                // Etapa 1: Lê o cabeçalho da textura original para saber o formato
                using (var fs = new FileStream(arquivoSelecionado, FileMode.Open, FileAccess.Read))
                using (var br = new BinaryReader(fs))
                {
                    br.BaseStream.Seek(baseOffset, SeekOrigin.Begin);
                    magic = br.ReadInt32();
                    offImg = br.ReadInt32();
                    offPal = br.ReadInt32();
                    br.ReadInt32(); // Pula tamanho
                    w = br.ReadUInt16();
                    h = br.ReadUInt16();
                }

                // Etapa 2: Gera os novos dados de paleta e pixels a partir da imagem PNG
                (byte[] novaPaletaLinear, byte[] novosIndices) = GerarDadosDeTextura(novaImagem, magic);

                // Etapa 3: Prepara os dados para serem gravados, aplicando swizzle/tiling conforme o formato
                byte[] dadosDePixelParaGravar;
                byte[] dadosDePaletaParaGravar;

                // Prepara os pixels
                if (magic == 0x7854324B) // PS2
                {
                    // A sua lógica original para decidir se aplica o swizzle dos pixels
                    bool precisaSwizzle = (offImg == 0x80);
                    dadosDePixelParaGravar = precisaSwizzle ? AplicarSwizzleIndices(novosIndices, w, h) : novosIndices;
                }
                else // PSP
                {
                    dadosDePixelParaGravar = AplicarTilingPsp(novosIndices, w, h);
                }

                // Prepara a paleta
                if (magic == 0x7854324B) // PS2
                {
                    dadosDePaletaParaGravar = SwizzlePaleta(novaPaletaLinear);
                }
                else // PSP
                {
                    dadosDePaletaParaGravar = novaPaletaLinear;
                }

                // Etapa 4: Grava os dados preparados de volta no arquivo
                using (var fs = new FileStream(arquivoSelecionado, FileMode.Open, FileAccess.Write))
                using (var bw = new BinaryWriter(fs))
                {
                    bw.BaseStream.Seek(baseOffset + offPal, SeekOrigin.Begin);
                    bw.Write(dadosDePaletaParaGravar);

                    bw.BaseStream.Seek(baseOffset + offImg, SeekOrigin.Begin);
                    bw.Write(dadosDePixelParaGravar);
                }

                ProcessarImagem(indice);
                MessageBox.Show("Imagem substituída com sucesso!", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro durante a substituição:\n{ex.Message}", "Erro Crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private (byte[] novaPaleta, byte[] novosIndices) GerarDadosDeTextura(Bitmap bmp, int magic)
        {
            var contagemCores = new Dictionary<Color, int>();
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    if (contagemCores.ContainsKey(px)) contagemCores[px]++;
                    else contagemCores[px] = 1;
                }
            }

            var paletaFinal = contagemCores.Keys.OrderByDescending(c => contagemCores[c]).Take(256).ToList();
            var coresExistentes = new HashSet<Color>(paletaFinal);
            int p = 0;
            while (paletaFinal.Count < 256)
            {
                Color corPreenchimento = Color.FromArgb(0, p, p, p);
                if (!coresExistentes.Contains(corPreenchimento))
                {
                    paletaFinal.Add(corPreenchimento);
                    coresExistentes.Add(corPreenchimento);
                }
                p++;
                if (p > 255) break;
            }

            var mapaCores = paletaFinal.Select((cor, i) => new { cor, i }).ToDictionary(par => par.cor, par => (byte)par.i);
            var bytesPaleta = new byte[256 * 4];
            for (int i = 0; i < paletaFinal.Count; i++)
            {
                Color cor = paletaFinal[i];
                bytesPaleta[i * 4 + 0] = cor.R;
                bytesPaleta[i * 4 + 1] = cor.G;
                bytesPaleta[i * 4 + 2] = cor.B;
                if (magic == 0x7854324B) // K2Tx (PS2)
                {
                    bytesPaleta[i * 4 + 3] = (byte)(cor.A / 2);
                }
                else // T2p (PSP)
                {
                    bytesPaleta[i * 4 + 3] = cor.A;
                }
            }

            var bytesIndices = new byte[bmp.Width * bmp.Height];
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    if (!mapaCores.TryGetValue(px, out byte index))
                    {
                        double menorDist = double.MaxValue;
                        for (int i = 0; i < paletaFinal.Count; i++)
                        {
                            double d = DistanciaEntreCores(px, paletaFinal[i]);
                            if (d < menorDist)
                            {
                                menorDist = d;
                                index = (byte)i;
                            }
                        }
                    }
                    bytesIndices[y * bmp.Width + x] = index;
                }
            }
            return (bytesPaleta, bytesIndices);
        }
        private byte[] AplicarSwizzleIndices(byte[] dados, int largura, int altura)
        {
            var sw = new byte[dados.Length];
            for (int y = 0; y < altura; y++)
                for (int x = 0; x < largura; x++)
                {
                    int block = (y & ~0xF) * largura + (x & ~0xF) * 2;
                    int sel = (((y + 2) >> 2) & 1) * 4;
                    int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
                    int col = posY * largura * 2 + ((x + sel) & 7) * 4;
                    int bn = ((y >> 1) & 1) + ((x >> 2) & 2);
                    int id = block + col + bn;
                    if (id < sw.Length) sw[id] = dados[y * largura + x];
                }
            return sw;
        }

        private byte[] SwizzlePaleta(byte[] paletaLinear)
        {
            var swizzled = new byte[paletaLinear.Length];
            int entradas = paletaLinear.Length / 4;
            for (int p = 0; p < entradas; p++)
            {
                int pos = ((p & 231) + ((p & 8) << 1) + ((p & 16) >> 1));
                if (pos < entradas)
                {
                    Buffer.BlockCopy(paletaLinear, p * 4, swizzled, pos * 4, 4);
                }
            }
            return swizzled;
        }

        private byte[] AplicarTilingPsp(byte[] indicesLineares, int largura, int altura)
        {
            byte[] dadosEmTiles = new byte[indicesLineares.Length];
            int tileWidth = 16;
            int tileHeight = 8;
            int tilesX = largura / tileWidth;
            int tilesY = altura / tileHeight;
            int indiceNoArrayDeTiles = 0;
            for (int ty = 0; ty < tilesY; ty++)
            {
                for (int tx = 0; tx < tilesX; tx++)
                {
                    for (int y = 0; y < tileHeight; y++)
                    {
                        for (int x = 0; x < tileWidth; x++)
                        {
                            int pixelX_global = tx * tileWidth + x;
                            int pixelY_global = ty * tileHeight + y;
                            int indiceLinear = pixelY_global * largura + pixelX_global;
                            if (indiceLinear < indicesLineares.Length)
                            {
                                dadosEmTiles[indiceNoArrayDeTiles] = indicesLineares[indiceLinear];
                                indiceNoArrayDeTiles++;
                            }
                        }
                    }
                }
            }
            return dadosEmTiles;
        }

        private byte[] AplicarUnSwizzle(byte[] data, int w, int h)
        {
            var uns = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int block = (y & ~0xF) * w + (x & ~0xF) * 2;
                    int sel = (((y + 2) >> 2) & 1) * 4;
                    int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
                    int col = posY * w * 2 + ((x + sel) & 7) * 4;
                    int bn = ((y >> 1) & 1) + ((x >> 2) & 2);
                    int id = block + col + bn;
                    if (id < data.Length)
                    {
                        uns[y * w + x] = data[id];
                    }
                }
            }
            return uns;
        }
        /// <summary>
        /// Aplica o algoritmo de Swizzle/Unswizzle da PS2 nos dados dos pixels.
        /// O algoritmo é seu próprio inverso.
        /// </summary>
        private byte[] SwizzlePixels(byte[] dados, int largura, int altura)
        {
            var sw = new byte[dados.Length];
            for (int y = 0; y < altura; y++)
            {
                for (int x = 0; x < largura; x++)
                {
                    int block = (y & ~0xF) * largura + (x & ~0xF) * 2;
                    int sel = (((y + 2) >> 2) & 1) * 4;
                    int posY = (((y & ~3) >> 1) + (y & 1)) & 7;
                    int col = posY * largura * 2 + ((x + sel) & 7) * 4;
                    int bn = ((y >> 1) & 1) + ((x >> 2) & 2);
                    int id = block + col + bn;
                    if (id < sw.Length)
                    {
                        sw[id] = dados[y * largura + x];
                    }
                }
            }
            return sw;
        }
        /// <summary>
        /// Analisa um Bitmap, gera a melhor paleta de 256 cores possível e retorna
        /// tanto a paleta (em bytes RGBA) quanto os índices de pixel mapeados para essa nova paleta.
        /// </summary>
        private (byte[] novaPaleta, byte[] novosIndices) GerarPaletaEIndices(Bitmap bmp, int magic)
        {
            // ... (toda a lógica de contagem e criação da paleta continua igual até aqui) ...
            var contagemCores = new Dictionary<Color, int>();
            int w = bmp.Width, h = bmp.Height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    if (contagemCores.ContainsKey(px))
                        contagemCores[px]++;
                    else
                        contagemCores[px] = 1;
                }
            }
            var paletaFinal = contagemCores.Keys.OrderByDescending(c => contagemCores[c])
                                               .Take(256)
                                               .ToList();
            var coresExistentes = new HashSet<Color>(paletaFinal);
            int valorCorPadding = 0;
            while (paletaFinal.Count < 256)
            {
                Color corPreenchimento = Color.FromArgb(0, valorCorPadding, valorCorPadding, valorCorPadding);
                if (!coresExistentes.Contains(corPreenchimento))
                {
                    paletaFinal.Add(corPreenchimento);
                    coresExistentes.Add(corPreenchimento);
                }
                valorCorPadding++;
                if (valorCorPadding > 255) break;
            }

            var paletaSimulada = new List<Color>(paletaFinal.Count);
            foreach (var corOriginal in paletaFinal)
            {
                byte alpha8bitSimulado;
                if (magic == 0x7854324B) // K2Tx (PS2)
                {
                    byte alpha7bit = (byte)Math.Round(corOriginal.A * 127.0 / 255.0);
                    alpha8bitSimulado = (byte)Math.Round(alpha7bit * 255.0 / 127.0);
                }
                else // T2p (PSP)
                {
                    alpha8bitSimulado = corOriginal.A;
                }
                paletaSimulada.Add(Color.FromArgb(alpha8bitSimulado, corOriginal.R, corOriginal.G, corOriginal.B));
            }

            var bytesPaleta = new byte[256 * 4];
            for (int i = 0; i < paletaFinal.Count; i++)
            {
                Color cor = paletaFinal[i];
                bytesPaleta[i * 4 + 0] = cor.R;
                bytesPaleta[i * 4 + 1] = cor.G;
                bytesPaleta[i * 4 + 2] = cor.B;

                // --- AQUI ESTÁ A LÓGICA CONDICIONAL PARA O ALFA ---
                if (magic == 0x7854324B) // K2Tx (PS2) usa alfa 7-bit
                {
                    bytesPaleta[i * 4 + 3] = (byte)(cor.A / 2); // Ou a fórmula mais precisa
                }
                else // T2p (PSP) usa alfa 8-bit
                {
                    bytesPaleta[i * 4 + 3] = cor.A;
                }
            }

            var bytesIndices = new byte[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color px = bmp.GetPixel(x, y);
                    double menorDist = double.MaxValue;
                    byte melhorIndex = 0;
                    for (int i = 0; i < paletaSimulada.Count; i++)
                    {
                        double d = DistanciaEntreCores(px, paletaSimulada[i]);
                        if (d < menorDist)
                        {
                            menorDist = d;
                            melhorIndex = (byte)i;
                        }
                        if (d == 0) break;
                    }
                    bytesIndices[y * w + x] = melhorIndex;
                }
            }

            return (bytesPaleta, bytesIndices);
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

            if (comboBoxBinFiles.Items.Count > 0)
            {
                comboBoxBinFiles.SelectedIndex = 0;
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
                        if (K2Tx == 0x7854324B || K2Tx == 0x20703274) // 'K2Tx' ou 'T2p' em hexadecimal
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
            if (indice < 0 || indice >= enderecoK2TxList.Count) return null;
            Bitmap bmp = null;
            using (var fs = File.Open(arquivoSelecionado, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var br = new BinaryReader(fs))
            {
                br.BaseStream.Seek(enderecoK2TxList[indice], SeekOrigin.Begin);
                magic = br.ReadInt32();
                int offImg = br.ReadInt32();
                int offPal = br.ReadInt32();
                int tamImg = br.ReadInt32();
                ushort w = br.ReadUInt16(), h = br.ReadUInt16();

                br.BaseStream.Seek(enderecoK2TxList[indice] + offImg, SeekOrigin.Begin);
                byte[] imgData = br.ReadBytes(tamImg - offImg);
                br.BaseStream.Seek(enderecoK2TxList[indice] + offPal, SeekOrigin.Begin);
                byte[] palData = br.ReadBytes(tamImg - offPal);

                if (offImg == 0x80)
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
                        imgData = AplicarUnSwizzle(imgData, w, h);
                    }
                }

                if (magic == 0x7854324B)
                    bmp = CriarBitmapk2tx(w, h, imgData, palData);
                else if (magic == 0x20703274)
                    bmp = CriarBitmapP2t(w, h, imgData, palData);
            }
            return bmp;
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
        private Bitmap CriarBitmapk2tx(int largura, int altura, byte[] imagemBytes, byte[] paletaBytes)
        {
            Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);
            byte[] unswizzledPalette = new byte[1024];
            for (int p = 0; p < 256; p++)
            {
                int pos = ((p & 231) + ((p & 8) << 1) + ((p & 16) >> 1));
                if (p * 4 + 4 <= paletaBytes.Length && pos * 4 + 4 <= unswizzledPalette.Length)
                {
                    Buffer.BlockCopy(paletaBytes, p * 4, unswizzledPalette, pos * 4, 4);
                }
            }
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
            int tileWidth = 16;
            int tileHeight = 8;
            int tileSize = tileWidth * tileHeight;
            int tilesX = largura / tileWidth;
            int tilesY = altura / tileHeight;
            Bitmap bitmap = new Bitmap(largura, altura, PixelFormat.Format32bppArgb);
            for (int tileY = 0; tileY < tilesY; tileY++)
            {
                for (int tileX = 0; tileX < tilesX; tileX++)
                {
                    int tileIndex = (tileY * tilesX + tileX);
                    int baseIndex = tileIndex * tileSize;
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

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
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

        /// <summary>
        /// Calcula a distância euclidiana entre duas cores (usando os componentes RGBA)
        /// </summary>
        private double DistanciaEntreCores(Color c1, Color c2)
        {
            int dr = c1.R - c2.R;
            int dg = c1.G - c2.G;
            int db = c1.B - c2.B;
            int da = c1.A - c2.A;
            return Math.Sqrt(dr * dr + dg * dg + db * db + da * da);
        }
    }
}
