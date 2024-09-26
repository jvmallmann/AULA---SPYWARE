using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NAudio.Wave; // Biblioteca NAudio para captura de áudio
using System.Threading.Tasks;
using Microsoft.VisualBasic;

class Program
{
    private static System.Windows.Forms.Timer timer; // Especifica o Timer do Windows Forms
    private static bool capturing;
    private static NotifyIcon trayIcon; // Ícone da bandeja do sistema
    private static ContextMenuStrip trayMenu; // Menu de contexto para o ícone da bandeja
    private static WaveInEvent waveSource; // Para captura de áudio
    private static WaveFileWriter waveFile; // Para salvar o arquivo de áudio

    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();

        // Inicializa o ícone da bandeja do sistema
        trayMenu = new ContextMenuStrip();

        trayIcon = new NotifyIcon
        {
            Text = "Monitor de Tela e Áudio",
            Icon = new Icon(SystemIcons.Application, 40, 40), // Ícone padrão da aplicação
            ContextMenuStrip = trayMenu,
            Visible = true
        };


        // Intervalo de captura em milissegundos
        int captureInterval = 5000; // Captura a cada 5 segundos
        capturing = true;

        // Configura o Timer
        timer = new System.Windows.Forms.Timer(); // Especifica o Timer do Windows Forms
        timer.Interval = captureInterval;
        timer.Tick += Timer_Tick;
        timer.Start();


        // Mantém a aplicação em execução sem mostrar um formulário (rodando em segundo plano)
        Application.Run();
    }

    // Manipulador do evento de timer para capturar tela e áudio
    // responsável por executar as operações de captura de tela e áudio em intervalos regulares.

    private static void Timer_Tick(object sender, EventArgs e)
    {
        if (capturing)
        {
            string filePath = CaptureScreen(); // Captura a tela a cada intervalo
            StartAudioCapture(); // Captura o áudio
        }
    }

    // Captura a tela e salva no diretório especificado
    private static string CaptureScreen()
    {
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TESTE-MALLMANN");
        Directory.CreateDirectory(folderPath); // Cria a pasta se não existir

        // Captura a tela inteira
        Rectangle bounds = Screen.GetBounds(Point.Empty);
        string fileName = Path.Combine(folderPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
        {
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            // Salva o arquivo localmente
            bitmap.Save(fileName, ImageFormat.Png);
        }

        return fileName; // Retorna o caminho do arquivo salvo
    }


    // Função para capturar áudio do microfone
    private static void StartAudioCapture()
    {
        string folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "TESTE-MALLMANN");
        Directory.CreateDirectory(folderPath); // Cria a pasta se não existir

        string audioFileName = Path.Combine(folderPath, $"audio_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        waveSource = new WaveInEvent();
        waveSource.WaveFormat = new WaveFormat(44100, 1); // 44.1kHz, mono
        waveFile = new WaveFileWriter(audioFileName, waveSource.WaveFormat);

        waveSource.DataAvailable += (s, a) =>
        {
            if (waveFile != null) // Certifique-se de que o arquivo está corretamente inicializado
            {
                waveFile.Write(a.Buffer, 0, a.BytesRecorded);
                waveFile.Flush(); // Garante que os dados sejam gravados no disco
            }
        };

        waveSource.RecordingStopped += (s, a) =>
        {
            waveFile?.Dispose(); // Garante que o arquivo seja fechado corretamente
            waveFile = null;
            waveSource.Dispose();
            Console.WriteLine($"Áudio capturado e salvo como: {audioFileName}");

            // Inicia uma nova gravação após parar a atual
            StartAudioCapture();
        };

        waveSource.StartRecording();

        // Captura o áudio por 30 segundos
        Task.Delay(30000).ContinueWith(_ => waveSource.StopRecording());
    }

    // Função para encerrar o aplicativo ao selecionar "Sair" no menu da bandeja
    private static void OnExit(object sender, EventArgs e)
    {
        trayIcon.Visible = false; // Esconde o ícone da bandeja
        capturing = false; // Para a captura
        timer.Stop(); // Para o timer
        Application.Exit(); // Fecha o aplicativo
    }
}
