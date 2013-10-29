using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace statsd.net.shared.Encryption
{
  /// <summary>
  /// Written by Mud, found here: http://stackoverflow.com/questions/165808/simple-2-way-encryption-for-c-sharp/5518092#5518092
  /// </summary>
  public class SimplerAES
  {
    //private static byte [] key = { 123, 217, 19, 11, 24, 26, 85, 45, 114, 184, 27, 162, 37, 112, 222, 209, 241, 24, 175, 144, 173, 53, 196, 29, 24, 26, 17, 218, 131, 236, 53, 209 };
    //private static byte [] vector = { 146, 64, 191, 111, 23, 3, 113, 119, 231, 121, 221, 112, 79, 32, 114, 156 };
    private ICryptoTransform encryptor, decryptor;
    private UTF8Encoding encoder;

    public SimplerAES (byte[] key, byte[] vector)
    {
      RijndaelManaged rm = new RijndaelManaged();
      encryptor = rm.CreateEncryptor( key, vector );
      decryptor = rm.CreateDecryptor( key, vector );
      encoder = new UTF8Encoding();
    }

    public string Encrypt ( string unencrypted )
    {
      return Convert.ToBase64String( Encrypt( encoder.GetBytes( unencrypted ) ) );
    }

    public string Decrypt ( string encrypted )
    {
      return encoder.GetString( Decrypt( Convert.FromBase64String( encrypted ) ) );
    }

    public string EncryptToUrl ( string unencrypted )
    {
      return HttpUtility.UrlEncode( Encrypt( unencrypted ) );
    }

    public string DecryptFromUrl ( string encrypted )
    {
      return Decrypt( HttpUtility.UrlDecode( encrypted ) );
    }

    public byte [] Encrypt ( byte [] buffer )
    {
      return Transform( buffer, encryptor );
    }

    public byte [] Decrypt ( byte [] buffer )
    {
      return Transform( buffer, decryptor );
    }

    protected byte [] Transform ( byte [] buffer, ICryptoTransform transform )
    {
      MemoryStream stream = new MemoryStream();
      using ( CryptoStream cs = new CryptoStream( stream, transform, CryptoStreamMode.Write ) )
      {
        cs.Write( buffer, 0, buffer.Length );
      }
      return stream.ToArray();
    }
  }
}
