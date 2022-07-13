using MDD4All.SpecIF.DataProvider.Contracts;
using MDD4All.SpecIF.DataProvider.File;
using System;

namespace MDD4All.SpecIF.Generators.Terms.Test
{
    internal class Program
    {
        Program()
        {
            ISpecIfMetadataReader metadataReader = new SpecIfFileMetadataReader(@"d:\SpecIF_Metadata\1.2\");

            SpecIfTermGenerator specIfTermGenerator = new SpecIfTermGenerator(metadataReader);


            string[] rootPaths = { @"d:\work\github\SpecIF-Class-Definitions_dev\" };

            SpecIF.DataModels.SpecIF result = specIfTermGenerator.GenerateDoaminVocabulary(rootPaths);

            SpecIfFileReaderWriter.SaveSpecIfToFile(result, @"d:\SpecIF_Metadata\1.2\vocabulary.specif");
        }

        static void Main(string[] args)
        {
            new Program();
        }
    }
}
