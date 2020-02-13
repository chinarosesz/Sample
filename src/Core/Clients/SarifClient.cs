using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Visitors;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System;

namespace Sarif.Util.Console
{
    public class SarifClient
    {
        public SarifLog InsertSnippets(string rawSarifFile, string sourceLocation)
        {
            // Read original Sarif raw log
            string text = File.ReadAllText(rawSarifFile);
            SarifLog sarifLog = JsonConvert.DeserializeObject<SarifLog>(text);

            // Set up
            ArtifactLocation artifactLocation = new ArtifactLocation { UriBaseId = sourceLocation };
            Dictionary<string, ArtifactLocation> originalUriBaseIds = new Dictionary<string, ArtifactLocation> 
            {
                { "srcroot", artifactLocation }
            };

            // Insert code snippets
            OptionallyEmittedData dataToInsert = OptionallyEmittedData.ContextRegionSnippets | OptionallyEmittedData.ContextRegionSnippets | OptionallyEmittedData.FlattenedMessages;
            SarifLog readySarifLog = new InsertOptionalDataVisitor(dataToInsert, originalUriBaseIds).VisitSarifLog(sarifLog);

            foreach (Run run in readySarifLog.Runs)
            {
                foreach (Result results in run.Results)
                {
                    foreach (Location location in results.Locations)
                    {
                        location.PhysicalLocation.ArtifactLocation.Uri = new Uri(sourceLocation + location.PhysicalLocation.ArtifactLocation.Uri);
                    }

                    if (results.RelatedLocations != null)
                    {
                        foreach (var relatedLocation in results.RelatedLocations)
                        {
                            relatedLocation.PhysicalLocation.ArtifactLocation.Uri = new Uri(sourceLocation + relatedLocation.PhysicalLocation.ArtifactLocation.Uri);
                        }
                    }
                }

                foreach (Artifact artifact in run.Artifacts)
                {
                    artifact.Location.Uri = new Uri(sourceLocation + artifact.Location.Uri);
                }
            }

            System.Console.WriteLine(readySarifLog.Runs[0].Artifacts[0].Location.UriBaseId);
            System.Console.WriteLine(readySarifLog.Runs[0].Artifacts[0].Location.Uri);

            return readySarifLog;
        }

        public void WriteSarifLogToFile(SarifLog sarifLog, string outputFile)
        {
            JsonSerializerOptions serializerOptions = new JsonSerializerOptions { WriteIndented = true };
            string sarifContents = JsonConvert.SerializeObject(sarifLog, Formatting.Indented);
            File.WriteAllText(outputFile, sarifContents);
        }
    }
}
