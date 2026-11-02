using System;
using System.Collections.Generic;
using System.IO;

namespace Azimuth
{
    // Stage each replacement beside its destination so the final rename stays on one volume.
    internal sealed class GalaxyFileTransaction
    {
        private readonly Stack<Action> undo = new Stack<Action>();
        private readonly List<string> leftovers = new List<string>();

        private static string TemporaryName(string destination)
        {
            return destination + ".azimuth-" + Guid.NewGuid().ToString("N");
        }

        public void Copy(string source, string destination)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string staged = TemporaryName(destination);
            leftovers.Add(staged);
            File.Copy(source, staged);
            if (File.Exists(destination))
            {
                string backup = TemporaryName(destination);
                File.Replace(staged, destination, backup);
                leftovers.Add(backup);
                undo.Push(() => File.Replace(backup, destination, null));
            }
            else
            {
                File.Move(staged, destination);
                undo.Push(() => File.Delete(destination));
            }
        }

        public void Remove(string destination)
        {
            string backup = TemporaryName(destination);
            File.Move(destination, backup);
            leftovers.Add(backup);
            undo.Push(() => File.Move(backup, destination));
        }

        public static void Run(Action<GalaxyFileTransaction> action)
        {
            var transaction = new GalaxyFileTransaction();
            try
            {
                action(transaction);
            }
            catch (Exception original)
            {
                var errors = new List<string>();
                while (transaction.undo.Count != 0)
                {
                    try { transaction.undo.Pop()(); }
                    catch (Exception ex) { errors.Add(ex.Message); }
                }
                if (errors.Count != 0)
                    throw new IOException("Rollback incomplete. Keep the .azimuth-* recovery files. " +
                        original.Message + Environment.NewLine + string.Join(Environment.NewLine, errors), original);

                transaction.Cleanup();
                throw;
            }
            transaction.Cleanup();
        }

        private void Cleanup()
        {
            // A locked temporary file must not undo a completed transaction.
            foreach (string file in leftovers)
            {
                try { File.Delete(file); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
