using System.Globalization;

namespace MeowC;

public record TokenType(string Name)
{
	public override string ToString()
	{
		return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(Name.ToLowerInvariant());
	}
}