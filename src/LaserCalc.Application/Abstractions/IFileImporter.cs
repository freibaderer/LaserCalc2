namespace LaserCalc.Application.Abstractions;

public interface IFileImporter<out T>
{
    bool CanImport(string filePath);
    T Import(string filePath);
}
