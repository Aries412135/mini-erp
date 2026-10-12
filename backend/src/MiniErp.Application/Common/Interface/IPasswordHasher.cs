using System.Reflection.Metadata;

namespace MiniErp.Application.Common.Interface;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string passwordhash, string password);
}