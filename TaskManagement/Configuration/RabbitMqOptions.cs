using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    [Required] public string UserName { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
    [Required] public string ExchangeName { get; set; } = string.Empty;
    [Required] public string RoutingKey { get; set; } = string.Empty;
}