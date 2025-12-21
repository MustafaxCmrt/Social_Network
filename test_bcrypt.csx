using BCrypt.Net;

// Migration'daki hash
var hashedPassword = "$2a$11$hKvNNt0fHLsqvVVdKp8R9e2DfWQG3VqLz0rXG0.CqE.8FGvN.3rXe";

// Test edelim
Console.WriteLine($"Hash: {hashedPassword}");
Console.WriteLine($"'admin' şifresi için: {BCrypt.Verify("admin", hashedPassword)}");
Console.WriteLine($"'Admin123' şifresi için: {BCrypt.Verify("Admin123", hashedPassword)}");

// Yeni bir "admin" şifresi için hash oluşturalım
var newHash = BCrypt.HashPassword("admin");
Console.WriteLine($"\n'admin' şifresi için yeni hash:\n{newHash}");
