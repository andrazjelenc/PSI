using Microsoft.Research.SEAL;
using System;
using System.Collections.Generic;
using System.IO;

namespace PSI
{
    static class Scenario
    {
        public static List<int> Run(ulong polyModulusDegree, ulong plainModulus, int[] xs, int[] ys)
        {
            // for communication between client and server
            var parmsStream = new MemoryStream();
            var relinKeysStream = new MemoryStream();
            var dataStream = new MemoryStream();
            var resultsStream = new MemoryStream();

            // for client's internal use
            var secretKeyStream = new MemoryStream();

            {
                /*
                 * Client
                 */
                Console.WriteLine($"<Client> Client has {xs.Length} elements: [{string.Join(", ", xs)}].");
                using var parms = new EncryptionParameters(SchemeType.BFV)
                {
                    PolyModulusDegree = polyModulusDegree,
                    CoeffModulus = CoeffModulus.BFVDefault(polyModulusDegree),
                    PlainModulus = new Modulus(plainModulus)
                };

                using var context = new SEALContext(parms);

                using var keygen = new KeyGenerator(context);
                using var secretKey = keygen.SecretKey;
                keygen.CreatePublicKey(out var publicKey);
                keygen.CreateRelinKeys(out var relinKeys);

                var writer = new BinaryWriter(dataStream);
                writer.Write((int)xs.Length);

                using var encryptor = new Encryptor(context, publicKey);

                for (int i = 0; i < xs.Length; i++)
                {
                    using var xPlain = new Plaintext(xs[i].ToString("X"));
                    encryptor.Encrypt(xPlain).Save(dataStream);
                }
                dataStream.Seek(0, SeekOrigin.Begin);

                parms.Save(parmsStream);
                parmsStream.Seek(0, SeekOrigin.Begin);

                relinKeys.Save(relinKeysStream);
                relinKeysStream.Seek(0, SeekOrigin.Begin);

                secretKey.Save(secretKeyStream);
                secretKeyStream.Seek(0, SeekOrigin.Begin);

                Console.WriteLine("----------------------");
            }
            {
                /*
                 * Server
                 */
                Console.WriteLine($"<Server> Server has {ys.Length} elements: [{string.Join(", ", ys)}].");

                using var parms = new EncryptionParameters();
                parms.Load(parmsStream);
                parmsStream.Seek(0, SeekOrigin.Begin);

                var plainModulusValue = (int)parms.PlainModulus.Value;

                using var context = new SEALContext(parms);
                using var evaluator = new Evaluator(context);

                using var relinKeys = new RelinKeys();
                relinKeys.Load(context, relinKeysStream);

                var ysPlain = new Plaintext[ys.Length];
                for (int j = 0; j < ys.Length; j++)
                {
                    ysPlain[j] = new Plaintext(ys[j].ToString("X"));
                }

                var reader = new BinaryReader(dataStream);
                int dataSize = reader.ReadInt32();

                var writer = new BinaryWriter(resultsStream);
                writer.Write((int)dataSize);

                var rnd = new Random();
                for (int i = 0; i < dataSize; i++)
                {
                    using var xEncrypted = new Ciphertext();
                    xEncrypted.Load(context, dataStream);

                    var tmpEncrypted = new Ciphertext[ys.Length];
                    for (int j = 0; j < ys.Length; j++)
                    {
                        tmpEncrypted[j] = new Ciphertext();
                        evaluator.SubPlain(xEncrypted, ysPlain[j], tmpEncrypted[j]);
                    }
                    using var plainRandomText = new Plaintext(rnd.Next(1, plainModulusValue).ToString());

                    using var resultEncrypted = new Ciphertext();
                    evaluator.MultiplyMany(tmpEncrypted, relinKeys, resultEncrypted);
                    evaluator.MultiplyPlainInplace(resultEncrypted, plainRandomText);

                    resultEncrypted.Save(resultsStream);
                }
                resultsStream.Seek(0, SeekOrigin.Begin);

                Console.WriteLine("----------------------");
            }
            {
                /*
                 * Client
                 */
                using var parms = new EncryptionParameters();
                parms.Load(parmsStream);

                using var context = new SEALContext(parms);

                using var secretKey = new SecretKey();
                secretKey.Load(context, secretKeyStream);

                using var decryptor = new Decryptor(context, secretKey);

                var reader = new BinaryReader(resultsStream);
                int resultsSize = reader.ReadInt32();


                List<int> intersection = new List<int>();
                for (int i = 0; i < resultsSize; i++)
                {
                    using var resultEncrypted = new Ciphertext();
                    resultEncrypted.Load(context, resultsStream);

                    var noiseBudget = decryptor.InvariantNoiseBudget(resultEncrypted);

                    if (noiseBudget > 0)
                    {
                        Console.WriteLine($"Noise: {noiseBudget} bits");
                        using var result = new Plaintext();
                        decryptor.Decrypt(resultEncrypted, result);
                        if (result.IsZero == true)
                        {
                            intersection.Add(xs[i]);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Ciphertext has too much noise.");
                    }

                }
                Console.WriteLine($"<Client> Intersection contains {intersection.Count} elements: [{string.Join(", ", intersection)}]");
                Console.WriteLine("----------------------");
                return intersection;
            }
        }
    }
}
