using MonECC.Infrastructure.IO;

namespace MonECC.Application.Dtos;

public record KeyPair(long PrivateKey, Point PublicKey);
