namespace Community.VisualStudio.SourceGenerators;

public class GeneratedFile
{
    public GeneratedFile(string fileName, string code)
    {
        FileName = fileName;
        Code = code;
    }

    public string FileName { get; }

    public string Code { get; }
}
