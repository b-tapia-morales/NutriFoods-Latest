using Ardalis.SmartEnum;

namespace Domain.Enum;

public class Gender : SmartEnum<Gender>
{
    public static readonly Gender Male = new(nameof(Male), "Hombre", 1);
    public static readonly Gender Female = new(nameof(Female), "Mujer", 2);

    public Gender(string name, string nameDisplay, int value) : base(name, value)
    {
        NameDisplay = nameDisplay;
    }

    public string NameDisplay { get; set; }
}