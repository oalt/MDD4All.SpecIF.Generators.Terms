using MDD4All.SpecIF.DataProvider.Contracts;
using MDD4All.SpecIF.DataProvider.File;
using System;

namespace MDD4All.SpecIF.Generators.Terms.Test
{
    internal class Program
    {
        Program()
        {
            ISpecIfMetadataReader metadataReader = new SpecIfFileMetadataReader(@"c:\test\SpecIF_Metadata\1.2\");

            SpecIfTermGenerator specIfTermGenerator = new SpecIfTermGenerator(metadataReader);


            string[] rootPaths = { @"c:\Users\alto\work\github\SpecIF-Class-Definitions_dev\" };

            SpecIF.DataModels.SpecIF result = specIfTermGenerator.GenerateDoaminVocabulary(rootPaths);

            SpecIfFileReaderWriter.SaveSpecIfToFile(result, @"c:\test\SpecIF_Metadata\vocabulary_generated.specif");
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
