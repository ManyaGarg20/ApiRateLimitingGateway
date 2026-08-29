namespace BackendApi.DTOs;

public record CreateProductDto(string Name, decimal Price, int Stock);