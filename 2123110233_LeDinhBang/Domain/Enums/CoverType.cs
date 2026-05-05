using System.Text.Json.Serialization;

namespace BookStore.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CoverType
{
    Paperback,   // Bìa mềm
    Hardcover,   // Bìa cứng
    Spiral       // Bìa xoắn
}
