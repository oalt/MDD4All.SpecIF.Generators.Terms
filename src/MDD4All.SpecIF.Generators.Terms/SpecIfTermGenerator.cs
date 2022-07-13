using MDD4All.SpecIF.DataFactory;
using MDD4All.SpecIF.DataModels;
using MDD4All.SpecIF.DataProvider.Contracts;
using System.Collections.Generic;
using MDD4All.SpecIF.DataModels.Manipulation;
using System.IO;
using MDD4All.SpecIF.DataProvider.File;
using System.Linq;
using MDD4All.SpecIF.DataModels.Helpers;

namespace MDD4All.SpecIF.Generators.Terms
{
    public class SpecIfTermGenerator
    {

        private ISpecIfMetadataReader _metadataReader;

        private Dictionary<Key, Resource> _propertyValueTerms = new Dictionary<Key, Resource>();
        private Dictionary<Key, Resource> _dataTypeTerms = new Dictionary<Key, Resource>();
        private Dictionary<Key, Resource> _propertyClassTerms = new Dictionary<Key, Resource>();
        private Dictionary<Key, Resource> _resourceClassTerms = new Dictionary<Key, Resource>();
        private Dictionary<Key, Resource> _statementClassTerms = new Dictionary<Key, Resource>();

        public SpecIfTermGenerator(ISpecIfMetadataReader metadataReader)
        {
            _metadataReader = metadataReader;
        }

        public SpecIF.DataModels.SpecIF GenerateDoaminVocabulary(string[] rootPaths)
        {
            SpecIF.DataModels.SpecIF result = new SpecIF.DataModels.SpecIF();

            Resource rootResource = SpecIfDataFactory.CreateResource(new Key("RC-Hierarchy", "1.1"), _metadataReader);

            rootResource.SetPropertyValue("dcterms:title", "SpecIF Vocabulary", _metadataReader);

            result.Resources.Add(rootResource);

            Node rootNode = new Node
            {
                ID = SpecIfGuidGenerator.CreateNewSpecIfGUID(),
                ResourceReference = new Key(rootResource.ID, rootResource.Revision)
            };

            result.Hierarchies.Add(rootNode);

            SpecIF.DataModels.SpecIF allMetadata = new SpecIF.DataModels.SpecIF();

            foreach (string path in rootPaths)
            {

                DirectoryInfo classDefinitionRootDirectory = new DirectoryInfo(path);

                foreach (DirectoryInfo domainDirectoryInfo in classDefinitionRootDirectory.GetDirectories())
                {

                    if (!domainDirectoryInfo.Name.StartsWith("vocabulary") &&
                        !domainDirectoryInfo.Name.StartsWith("_") &&
                        !domainDirectoryInfo.Name.StartsWith(".git"))
                    {
                        DataModels.DomainInfo domainInfo = GetDomainInfoFromDirectoryName(domainDirectoryInfo.Name);

                        FileInfo[] specIfFiles = domainDirectoryInfo.GetFiles("*.specif");

                        SpecIF.DataModels.SpecIF domainSpecIF = new SpecIF.DataModels.SpecIF();

                        foreach (FileInfo fileInfo in specIfFiles)
                        {

                            SpecIF.DataModels.SpecIF specIF = SpecIfFileReaderWriter.ReadDataFromSpecIfFile(fileInfo.FullName);

                            foreach (DataType dataType in specIF.DataTypes)
                            {
                                if (!domainSpecIF.DataTypes.Any(el => el.ID == dataType.ID && el.Revision == dataType.Revision))
                                {
                                    domainSpecIF.DataTypes.Add(dataType);
                                }
                            }

                            foreach (PropertyClass propertyClass in specIF.PropertyClasses)
                            {
                                if (!domainSpecIF.PropertyClasses.Any(el => el.ID == propertyClass.ID && el.Revision == propertyClass.Revision))
                                {
                                    domainSpecIF.PropertyClasses.Add(propertyClass);
                                }
                            }

                            foreach (ResourceClass resourceClass in specIF.ResourceClasses)
                            {
                                if (!domainSpecIF.ResourceClasses.Any(el => el.ID == resourceClass.ID && el.Revision == resourceClass.Revision))
                                {
                                    domainSpecIF.ResourceClasses.Add(resourceClass);
                                }
                            }

                            foreach (StatementClass statementClass in specIF.StatementClasses)
                            {
                                if (!domainSpecIF.StatementClasses.Any(el => el.ID == statementClass.ID && el.Revision == statementClass.Revision))
                                {
                                    domainSpecIF.StatementClasses.Add(statementClass);
                                }
                            }

                        }

                        allMetadata.DataTypes.AddRange(domainSpecIF.DataTypes);
                        allMetadata.PropertyClasses.AddRange(domainSpecIF.PropertyClasses);
                        allMetadata.ResourceClasses.AddRange(domainSpecIF.ResourceClasses);
                        allMetadata.StatementClasses.AddRange(domainSpecIF.StatementClasses);

                        SpecIF.DataModels.SpecIF domainTerms = GenerateTermDefinitionFromMetadata(domainSpecIF, domainInfo);

                        result.Resources.AddRange(domainTerms.Resources);
                        result.Statements.AddRange(domainTerms.Statements);

                        if (domainTerms.Hierarchies.Any())
                        {
                            rootNode.AddChildNode(domainTerms.Hierarchies[0]);
                        }
                    }


                }

            }

            List<Statement> dataTypeStatements = GenerateDataTypeStatements(allMetadata);
            result.Statements.AddRange(dataTypeStatements);
            List<Statement> propertyClassStatements = GeneratePropertyClassStatements(allMetadata);
            result.Statements.AddRange(propertyClassStatements);
            List<Statement> resourceClassStatements = GenerateResourceClassStatements(allMetadata);
            result.Statements.AddRange(resourceClassStatements);
            List<Statement> statementClassStatements = GenerateStatementClassStatements(allMetadata);
            result.Statements.AddRange(statementClassStatements);

            return result;
        }

        private DataModels.DomainInfo GetDomainInfoFromDirectoryName(string directoryName)
        {
            DataModels.DomainInfo result = new DataModels.DomainInfo();

            switch (directoryName)
            {
                case "01_Base Definitions":
                    result = new DataModels.DomainInfo
                    {
                        DomainEnumerationValueID = "V-Domain-1"
                    };
                    break;

                case "02_Requirements Engineering":
                    result = new DataModels.DomainInfo
                    {
                        DomainEnumerationValueID = "V-Domain-2"
                    };
                    break;

                case "03_Model Integration":
                    result = new DataModels.DomainInfo
                    {
                        DomainEnumerationValueID = "V-Domain-4"
                    };
                    break;

                case "04_Automotive Requirements Engineering":
                    result = new DataModels.DomainInfo
                    {
                        DomainEnumerationValueID = "V-Domain-3"
                    };
                    break;


                case "10_Vocabulary Definition":
                    result = new DataModels.DomainInfo
                    {
                        DomainEnumerationValueID = "V-Domain-39"
                    };
                    break;

            }

            result.Title = directoryName.Replace("_", " ");


            return result;
        }

        private SpecIF.DataModels.SpecIF GenerateTermDefinitionFromMetadata(SpecIF.DataModels.SpecIF metadata,
                                                                            DataModels.DomainInfo domainInfo = null)
        {
            SpecIF.DataModels.SpecIF result = new SpecIF.DataModels.SpecIF();

            Resource domainFolder = SpecIfDataFactory.CreateResource(new Key("RC-Folder", "1.1"), _metadataReader);

            if (domainInfo != null)
            {
                domainFolder.SetPropertyValue("dcterms:title", domainInfo.Title, _metadataReader);
            }
            
            result.Resources.Add(domainFolder);

            Node domainNode = new Node
            {
                ResourceReference = new Key(domainFolder.ID, domainFolder.Revision)
            };

            result.Hierarchies.Add(domainNode);

            // property value & data type terms
            Dictionary<Key, Resource> valueResources = new Dictionary<Key, Resource>();

            Dictionary<Key, Resource> dataTypeTerms = GenerateDataTypeTerms(metadata, out valueResources);

            if (valueResources.Count > 0)
            {
                Resource valueFolder = SpecIfDataFactory.CreateResource(new Key("RC-Folder", "1.1"), _metadataReader);

                valueFolder.SetPropertyValue("dcterms:title", "Property Value Terms", _metadataReader);

                result.Resources.Add(valueFolder);

                Node valueFolderNode = new Node
                {
                    ResourceReference = new Key(valueFolder.ID, valueFolder.Revision)
                };

                domainNode.Nodes.Add(valueFolderNode);

                foreach (KeyValuePair<Key, Resource> keyValuePair in valueResources)
                {
                    if (!_propertyValueTerms.ContainsKey(keyValuePair.Key))
                    {
                        _propertyValueTerms.Add(keyValuePair.Key, keyValuePair.Value);
                        result.Resources.Add(keyValuePair.Value);

                        valueFolderNode.AddChildNode(keyValuePair.Value);
                    }

                }
            }

            if (dataTypeTerms.Count > 0)
            {
                Node dataTypeFolderNode;
                Resource dataTypeFolder = SpecIfDataFactory.CreateResourceWithNode(new Key("RC-Folder", "1.1"),
                                                                                   out dataTypeFolderNode,
                                                                                   _metadataReader);

                dataTypeFolder.SetPropertyValue("dcterms:title", "Data Type Terms", _metadataReader);

                result.Resources.Add(dataTypeFolder);

                domainNode.AddChildNode(dataTypeFolderNode);

                foreach (KeyValuePair<Key, Resource> keyValuePair in dataTypeTerms)
                {
                    if (!_dataTypeTerms.ContainsKey(keyValuePair.Key))
                    {
                        _dataTypeTerms.Add(keyValuePair.Key, keyValuePair.Value);
                        result.Resources.Add(keyValuePair.Value);

                        dataTypeFolderNode.AddChildNode(keyValuePair.Value);
                    }

                }

            }

            // property class terms
            Dictionary<Key, Resource> propertyClassTerms = GeneratePropertyClassTerms(metadata);

            if (propertyClassTerms.Count > 0)
            {
                Node propertyClassFolderNode;
                Resource propertyClassFolder = SpecIfDataFactory.CreateResourceWithNode(new Key("RC-Folder", "1.1"),
                                                                                   out propertyClassFolderNode,
                                                                                   _metadataReader);

                propertyClassFolder.SetPropertyValue("dcterms:title", "Property Class Terms", _metadataReader);

                result.Resources.Add(propertyClassFolder);

                domainNode.AddChildNode(propertyClassFolderNode);

                foreach (KeyValuePair<Key, Resource> keyValuePair in propertyClassTerms)
                {
                    if (!_propertyClassTerms.ContainsKey(keyValuePair.Key))
                    {
                        _propertyClassTerms.Add(keyValuePair.Key, keyValuePair.Value);
                        result.Resources.Add(keyValuePair.Value);

                        propertyClassFolderNode.AddChildNode(keyValuePair.Value);
                    }
                }
            }


            // resource class terms
            Dictionary<Key, Resource> resourceClassTerms = GenerateResourceClassTerms(metadata);

            if (resourceClassTerms.Count > 0)
            {
                Node resourceClassFolderNode;
                Resource resourceClassFolder = SpecIfDataFactory.CreateResourceWithNode(new Key("RC-Folder", "1.1"),
                                                                                   out resourceClassFolderNode,
                                                                                   _metadataReader);

                resourceClassFolder.SetPropertyValue("dcterms:title", "Resource Class Terms", _metadataReader);

                result.Resources.Add(resourceClassFolder);

                domainNode.AddChildNode(resourceClassFolderNode);

                foreach (KeyValuePair<Key, Resource> keyValuePair in resourceClassTerms)
                {
                    if (!_resourceClassTerms.ContainsKey(keyValuePair.Key))
                    {
                        _resourceClassTerms.Add(keyValuePair.Key, keyValuePair.Value);
                        result.Resources.Add(keyValuePair.Value);

                        resourceClassFolderNode.AddChildNode(keyValuePair.Value);
                    }
                }

            }


            // statement class terms
            Dictionary<Key, Resource> statementClassTerms = GenerateStatementClassTerms(metadata);

            if (statementClassTerms.Count > 0)
            {
                Node statementClassFolderNode;
                Resource statementClassFolder = SpecIfDataFactory.CreateResourceWithNode(new Key("RC-Folder", "1.1"),
                                                                                   out statementClassFolderNode,
                                                                                   _metadataReader);

                statementClassFolder.SetPropertyValue("dcterms:title", "Statement Class Terms", _metadataReader);

                result.Resources.Add(statementClassFolder);

                domainNode.AddChildNode(statementClassFolderNode);

                foreach (KeyValuePair<Key, Resource> keyValuePair in statementClassTerms)
                {
                    if (!_statementClassTerms.ContainsKey(keyValuePair.Key))
                    {
                        _statementClassTerms.Add(keyValuePair.Key, keyValuePair.Value);
                        result.Resources.Add(keyValuePair.Value);

                        statementClassFolderNode.AddChildNode(keyValuePair.Value);
                    }
                }
            }

            return result;
        }

        private Dictionary<Key, Resource> GenerateDataTypeTerms(SpecIF.DataModels.SpecIF metadata,
                                                                out Dictionary<Key, Resource> valueResources,
                                                                DataModels.DomainInfo domainInfo = null)
        {
            Dictionary<Key, Resource> result = new Dictionary<Key, Resource>();

            valueResources = new Dictionary<Key, Resource>();

            foreach (DataType dataType in metadata.DataTypes)
            {

                Resource termDataType = SpecIfDataFactory.CreateResource(new Key("RC-TermDataType", "1.2"), _metadataReader);

                termDataType.ID = "TDT-" + dataType.ID;
                termDataType.Revision = "1";

                // title
                termDataType.SetPropertyValue(new Key("PC-Name", "1.1"), new Value(dataType.Title));

                termDataType.SetPropertyValue(new Key("PC-VisibleId", "1.1"), new Value(dataType.ID));

                termDataType.SetPropertyValue(new Key("PC-Revision", "1.2"), new Value(dataType.Revision));

                termDataType.SetPropertyValue(new Key("PC-Type", "1.1"), new Value(dataType.Type));

                termDataType.SetPropertyValue(new Key("PC-Description", "1.1"), new Value(dataType.Description[0]));

                if(dataType.MinInclusive != null)
                {
                    termDataType.SetPropertyValue(new Key("PC-MinInclusive", "1.2"), new Value(dataType.MinInclusive.ToString()));
                }

                if(dataType.MaxInclusive != null)
                {
                    termDataType.SetPropertyValue(new Key("PC-MaxInclusive", "1.2"), new Value(dataType.MaxInclusive.ToString()));
                }

                if(dataType.FractionDigits != null)
                {
                    termDataType.SetPropertyValue(new Key("PC-FractionDigits", "1.2"), new Value(dataType.FractionDigits.ToString()));
                }

                SetDomainProperty(termDataType, domainInfo);

                if (dataType.Enumeration != null && dataType.Enumeration.Count > 0)
                {
                    termDataType.SetPropertyValue(new Key("PC-IsEnumeration", "1.2"), new Value("true"));

                    foreach (EnumerationValue enumReference in dataType.Enumeration)
                    {
                        Resource enumValueTerm = SpecIfDataFactory.CreateResource(new Key("RC-TermPropertyValue", "1.2"),
                                                                                  _metadataReader);

                        enumValueTerm.ID = "TPV-" + enumReference.ID;
                        enumValueTerm.Revision = "1";

                        Value enumTextValue = new Value();

                        foreach (MultilanguageText enumText in enumReference.Value)
                        {
                            enumTextValue.MultilanguageTexts.Add(enumText);
                        }

                        enumValueTerm.SetPropertyValue(new Key("PC-Name", "1.1"), enumTextValue);

                        enumValueTerm.SetPropertyValue(new Key("PC-VisibleId", "1.1"), new Value(enumReference.ID));

                        //enumValueTerm.SetPropertyValue(new Key("PC-Description", "1.1"), enumTextValue);

                        SetDomainProperty(enumValueTerm, domainInfo);

                        valueResources.Add(new Key(enumReference.ID), enumValueTerm);



                    }
                }
                else
                {
                    termDataType.SetPropertyValue(new Key("PC-IsEnumeration", "1.2"), new Value("false"));
                }

                result.Add(new Key(dataType.ID, dataType.Revision), termDataType);
            }

            return result;
        }


        private List<Statement> GenerateDataTypeStatements(SpecIF.DataModels.SpecIF metadata)
        {
            List<Statement> result = new List<Statement>();

            foreach (DataType dataType in metadata.DataTypes)
            {
                Resource termDataType = _dataTypeTerms[new Key(dataType.ID, dataType.Revision)];

                if (dataType.Enumeration != null && dataType.Enumeration.Count > 0)
                {
                    foreach (EnumerationValue enumReference in dataType.Enumeration)
                    {
                        Resource enumValueTerm = _propertyValueTerms[new Key(enumReference.ID)];

                        Statement containsStatement = SpecIfDataFactory.CreateStatement(new Key("SC-contains", "1.1"),
                                                                                            new Key(termDataType.ID, termDataType.Revision),
                                                                                            new Key(enumValueTerm.ID, enumValueTerm.Revision),
                                                                                            _metadataReader);

                        result.Add(containsStatement);
                    }
                }
            }
            return result;
        }

        private Dictionary<Key, Resource> GeneratePropertyClassTerms(SpecIF.DataModels.SpecIF metadata,
                                                                     DataModels.DomainInfo domainInfo = null)
        {
            Dictionary<Key, Resource> result = new Dictionary<Key, Resource>();


            foreach (PropertyClass propertyClass in metadata.PropertyClasses)
            {

                Resource termPropertyClass = SpecIfDataFactory.CreateResource(new Key("RC-TermPropertyClass", "1.2"),
                                                                              _metadataReader);

                termPropertyClass.ID = "TPC-" + propertyClass.ID;
                termPropertyClass.Revision = "1";

                // title
                termPropertyClass.SetPropertyValue(new Key("PC-Name", "1.1"), new Value(propertyClass.Title));

                termPropertyClass.SetPropertyValue(new Key("PC-VisibleId", "1.1"), new Value(propertyClass.ID));

                termPropertyClass.SetPropertyValue(new Key("PC-Revision", "1.2"), new Value(propertyClass.Revision));

                termPropertyClass.SetPropertyValue(new Key("PC-Description", "1.1"), new Value(propertyClass.Description.GetDefaultStringValue()));

                termPropertyClass.SetPropertyValue(new Key("PC-Unit", "1.2"), new Value(propertyClass.Unit));

                termPropertyClass.SetPropertyValue(new Key("PC-Multiple", "1.2"), new Value(propertyClass.Multiple.ToString()));

                SetDomainProperty(termPropertyClass, domainInfo);

                result.Add(new Key(propertyClass.ID, propertyClass.Revision), termPropertyClass);

            }

            return result;
        }

        private List<Statement> GeneratePropertyClassStatements(SpecIF.DataModels.SpecIF metadata)
        {
            List<Statement> result = new List<Statement>();

            foreach (PropertyClass propertyClass in metadata.PropertyClasses)
            {
                Resource dataTypeTerm = _dataTypeTerms[new Key(propertyClass.DataType.ID, propertyClass.DataType.Revision)];

                Resource propertyClassTerm = _propertyClassTerms[new Key(propertyClass.ID, propertyClass.Revision)];

                Statement typeStatement = SpecIfDataFactory.CreateStatement(new Key("SC-Classifier", "1.1"),
                                                                            new Key(propertyClassTerm.ID, propertyClassTerm.Revision),
                                                                            new Key(dataTypeTerm.ID, dataTypeTerm.Revision),
                                                                            _metadataReader);

                result.Add(typeStatement);
            }

            return result;
        }


        private Dictionary<Key, Resource> GenerateResourceClassTerms(SpecIF.DataModels.SpecIF metadata,
                                                                     DataModels.DomainInfo domainInfo = null)
        {
            Dictionary<Key, Resource> result = new Dictionary<Key, Resource>();

            foreach (ResourceClass resourceClass in metadata.ResourceClasses)
            {

                Resource termResourceClass = SpecIfDataFactory.CreateResource(new Key("RC-TermResourceClass", "1.2"),
                                                                              _metadataReader);

                termResourceClass.ID = "TRC-" + resourceClass.ID;
                termResourceClass.Revision = "1";

                // title
                termResourceClass.SetPropertyValue(new Key("PC-Name", "1.1"), new Value(resourceClass.Title));

                termResourceClass.SetPropertyValue(new Key("PC-VisibleId", "1.1"), new Value(resourceClass.ID));

                termResourceClass.SetPropertyValue(new Key("PC-Revision", "1.2"), new Value(resourceClass.Revision));

                termResourceClass.SetPropertyValue(new Key("PC-Description", "1.1"), new Value(resourceClass.Description.GetDefaultStringValue()));

                termResourceClass.SetPropertyValue(new Key("PC-IsHeading", "1.2"), new Value(resourceClass.isHeading.ToString()));

                if(resourceClass.Instantiation != null && resourceClass.Instantiation.Count() > 0)
                {
                    Value value = new Value();

                    string valueText = "";

                    List<string> enumIDs = new List<string>();

                    foreach(string instantiationValue in resourceClass.Instantiation)
                    {
                        if(instantiationValue == "auto")
                        {
                            enumIDs.Add("V-Instantiation-1");
                            
                        }
                        if(instantiationValue == "user")
                        {
                            enumIDs.Add("V-Instantiation-0");
                        }
                    }

                    for(int counter = 0; counter < enumIDs.Count; counter++)
                    {
                        valueText = enumIDs[counter];

                        if(counter < enumIDs.Count - 1)
                        {
                            valueText += ", ";
                        }
                    }


                    termResourceClass.SetPropertyValue(new Key("PC-Instantiation", "1"), new Value(valueText));
                }

                

                SetDomainProperty(termResourceClass, domainInfo);


                result.Add(new Key(resourceClass.ID, resourceClass.Revision), termResourceClass);

            }

            return result;
        }

        private List<Statement> GenerateResourceClassStatements(SpecIF.DataModels.SpecIF metadata)
        {
            List<Statement> result = new List<Statement>();

            foreach (ResourceClass resourceClass in metadata.ResourceClasses)
            {
                for (int counter = 0; counter < resourceClass.PropertyClasses.Count; counter++)
                {
                    Resource propertyClassTerm = _propertyClassTerms[resourceClass.PropertyClasses[counter]];

                    Resource resourceClassTerm = _resourceClassTerms[new Key(resourceClass.ID, resourceClass.Revision)];

                    if (propertyClassTerm != null)
                    {
                        Statement orderedContainsStatement = SpecIfDataFactory.CreateStatement(new Key("SC-orderedContains", "1.2"),
                                                                            new Key(resourceClassTerm.ID, resourceClassTerm.Revision),
                                                                            new Key(propertyClassTerm.ID, propertyClassTerm.Revision),
                                                                            _metadataReader);

                        orderedContainsStatement.SetPropertyValue(new Key("PC-Index", "1.2"), new Value(counter.ToString()));

                        result.Add(orderedContainsStatement);
                    }
                }
            }

            return result;
        }

        private Dictionary<Key, Resource> GenerateStatementClassTerms(SpecIF.DataModels.SpecIF metadata,
                                                                      DataModels.DomainInfo domainInfo = null)
        {
            Dictionary<Key, Resource> result = new Dictionary<Key, Resource>();

            foreach (StatementClass statementClass in metadata.StatementClasses)
            {

                Resource termStatementClass = SpecIfDataFactory.CreateResource(new Key("RC-TermStatementClass", "1.2"),
                                                                              _metadataReader);

                termStatementClass.ID = "TSC-" + statementClass.ID;
                termStatementClass.Revision = "1";

                // title
                termStatementClass.SetPropertyValue(new Key("PC-Name", "1.1"), new Value(statementClass.Title));

                termStatementClass.SetPropertyValue(new Key("PC-VisibleId", "1.1"), new Value(statementClass.ID));

                termStatementClass.SetPropertyValue(new Key("PC-Revision", "1.2"), new Value(statementClass.Revision));

                termStatementClass.SetPropertyValue(new Key("PC-Description", "1.1"), new Value(statementClass.Description.GetDefaultStringValue()));

                termStatementClass.SetPropertyValue(new Key("PC-IsHeading", "1.2"), new Value(statementClass.isHeading.ToString()));

                if (statementClass.Instantiation != null && statementClass.Instantiation.Count() > 0)
                {
                    Value value = new Value();

                    string valueText = "";

                    List<string> enumIDs = new List<string>();

                    foreach (string instantiationValue in statementClass.Instantiation)
                    {
                        if (instantiationValue == "auto")
                        {
                            enumIDs.Add("V-Instantiation-1");

                        }
                        if (instantiationValue == "user")
                        {
                            enumIDs.Add("V-Instantiation-0");
                        }
                    }

                    for (int counter = 0; counter < enumIDs.Count; counter++)
                    {
                        valueText = enumIDs[counter];

                        if (counter < enumIDs.Count - 1)
                        {
                            valueText += ", ";
                        }
                    }


                    termStatementClass.SetPropertyValue(new Key("PC-Instantiation", "1"), new Value(valueText));
                }

                SetDomainProperty(termStatementClass, domainInfo);

                result.Add(new Key(statementClass.ID, statementClass.Revision), termStatementClass);
            }

            return result;
        }


        private List<Statement> GenerateStatementClassStatements(SpecIF.DataModels.SpecIF metadata)
        {
            List<Statement> result = new List<Statement>();

            foreach (StatementClass statementClass in metadata.StatementClasses)
            {
                Resource termStatementClass = _statementClassTerms[new Key(statementClass.ID, statementClass.Revision)];

                if (statementClass.PropertyClasses != null)
                {
                    for (int counter = 0; counter < statementClass.PropertyClasses.Count; counter++)
                    {
                        Resource propertyClassTerm = _propertyClassTerms[statementClass.PropertyClasses[counter]];



                        if (propertyClassTerm != null)
                        {
                            Statement orderedContainsStatement = SpecIfDataFactory.CreateStatement(new Key("SC-orderedContains", "1.2"),
                                                                                new Key(termStatementClass.ID, termStatementClass.Revision),
                                                                                new Key(propertyClassTerm.ID, propertyClassTerm.Revision),
                                                                                _metadataReader);

                            orderedContainsStatement.SetPropertyValue(new Key("PC-Index", "1.2"), new Value(counter.ToString()));

                            result.Add(orderedContainsStatement);
                        }
                    }


                    if (statementClass.SubjectClasses != null)
                    {
                        foreach (Key subjectClassKey in statementClass.SubjectClasses)
                        {
                            Resource subject = null;
                            if (_resourceClassTerms.ContainsKey(subjectClassKey))
                            {
                                subject = _resourceClassTerms[subjectClassKey];
                            }
                            else
                            {
                                if (subjectClassKey.Revision == null) // key does not point to a specific revision
                                {
                                    foreach (KeyValuePair<Key, Resource> keyValuePair in _resourceClassTerms)
                                    {
                                        if (keyValuePair.Key.ID == subjectClassKey.ID)
                                        {
                                            subject = keyValuePair.Value;
                                            break;
                                        }
                                    }
                                }
                            }

                            if (subject != null)
                            {
                                Statement subjectStatement = SpecIfDataFactory.CreateStatement(new Key("SC-isEligibleAsSubject", "1.2"),
                                                                                               new Key(termStatementClass.ID, termStatementClass.Revision),
                                                                                               new Key(subject.ID, subject.Revision),
                                                                                               _metadataReader);

                                result.Add(subjectStatement);
                            }
                        }
                    }

                    if (statementClass.ObjectClasses != null)
                    {
                        foreach (Key objectClassKey in statementClass.ObjectClasses)
                        {
                            Resource objectTerm = null;
                            if (_resourceClassTerms.ContainsKey(objectClassKey))
                            {
                                objectTerm = _resourceClassTerms[objectClassKey];
                            }
                            else
                            {
                                if (objectClassKey.Revision == null) // key does not point to a specific revision
                                {
                                    foreach (KeyValuePair<Key, Resource> keyValuePair in _resourceClassTerms)
                                    {
                                        if (keyValuePair.Key.ID == objectClassKey.ID)
                                        {
                                            objectTerm = keyValuePair.Value;
                                            break;

                                        }
                                    }
                                }
                            }
                            if (objectTerm != null)
                            {
                                Statement objectStatement = SpecIfDataFactory.CreateStatement(new Key("SC-isEligibleAsObject", "1.2"),
                                                                                              new Key(termStatementClass.ID, termStatementClass.Revision),
                                                                                              new Key(objectTerm.ID, objectTerm.Revision),
                                                                                              _metadataReader);

                                result.Add(objectStatement);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void SetDomainProperty(Resource resource, DataModels.DomainInfo domainInfo)
        {
            if (domainInfo != null && !string.IsNullOrEmpty(domainInfo.DomainEnumerationValueID))
            {
                resource.SetPropertyValue(new Key("PC-Domain", "1.2"), new Value(domainInfo.DomainEnumerationValueID));
            }
        }
    }
}
