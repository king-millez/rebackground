using rebackground;
using System.CommandLine;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Option<FileInfo> InputFile = new Option<FileInfo>(
            name: "-i",
            description: "The input image file."
        );
        Option<FileInfo> OutputFile = new Option<FileInfo>(
            name: "-o",
            description: "The output image file with an edited background."
        );
        Option<string> DallePrompt = new Option<string>(
            name: "-p",
            description: "The DALL-E prompt for the new background."
        );
        RootCommand rootCommand = new RootCommand("Replace the background of (almost) any image.");
        rootCommand.AddOption(InputFile);
        rootCommand.AddOption(OutputFile);
        rootCommand.AddOption(DallePrompt);

        rootCommand.SetHandler(
            async (input, output, prompt) =>
            {
                await RunApp(input, output, prompt);
            },
            InputFile,
            OutputFile,
            DallePrompt
        );
        return await rootCommand.InvokeAsync(args);
    }

    static async Task RunApp(FileInfo input, FileInfo output, string prompt)
    {
        if (input == null)
        {
            throw new ArgumentNullException($"An input image must be specified.");
        }
        else if (output == null)
        {
            throw new ArgumentNullException("An output image must be specified.");
        }
        else if (prompt == null)
        {
            throw new ArgumentNullException("A DALL-E prompt must be specified.");
        }
        string UserDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        string parentDirectoryPath = Path.Combine(UserDir, ".removebackground");
        await Task.Run(() =>
        {
            if (!Directory.Exists(parentDirectoryPath))
            {
                Directory.CreateDirectory(parentDirectoryPath);
            }
        });

        ImageHandler imageHandler = new ImageHandler();
        string noBg;
        Image maskImage;
        byte[] imageContent = await File.ReadAllBytesAsync(input.ToString());
        long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        string uuid = Guid.NewGuid().ToString();
        string nestedDirectoryPath = Path.Combine(parentDirectoryPath, $"{unixTimestamp}-{uuid}");
        await Task.Run(() => Directory.CreateDirectory(nestedDirectoryPath));

        Console.WriteLine($"[+] Created directory: {nestedDirectoryPath}");
        (noBg, maskImage) = await imageHandler.RemoveBackgroundAsync(
            input.ToString(),
            imageContent,
            nestedDirectoryPath
        );
        await Console.Out.WriteLineAsync("[+] Converting and resizing mask");
        Image origImage = Image.Load(imageContent);
        // In the event remove.bg makes the mask image smaller, give it the original image's size.
        // We also want to convert the original image to PNG
        maskImage.Mutate(x => x.Resize(origImage.Width, origImage.Height));
        using MemoryStream MaskPngMs = new MemoryStream();
        await maskImage.SaveAsPngAsync(MaskPngMs);
        using MemoryStream InputPngMs = new MemoryStream();
        await origImage.SaveAsPngAsync(InputPngMs);
        await imageHandler.FillBackgroundAsync(
            InputPngMs.ToArray(),
            MaskPngMs.ToArray(),
            $"{Path.GetFileNameWithoutExtension(input.ToString())}.png",
            Path.GetFileName(noBg),
            output.ToString(),
            prompt,
            1,
            $"1024x1024"
        );
    }
}
