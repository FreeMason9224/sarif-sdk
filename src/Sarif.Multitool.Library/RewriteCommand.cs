﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.Driver.Sdk;
using Microsoft.CodeAnalysis.Sarif.Visitors;

namespace Microsoft.CodeAnalysis.Sarif.Multitool
{
    public class RewriteCommand : CommandBase
    {
        private readonly IFileSystem _fileSystem;

        public RewriteCommand(IFileSystem fileSystem = null)
        {
            _fileSystem = fileSystem ?? FileSystem.Instance;
        }

        public int Run(RewriteOptions rewriteOptions)
        {
            try
            {
                Console.WriteLine($"Rewriting '{rewriteOptions.InputFilePath}' => '{rewriteOptions.OutputFilePath}'...");
                Stopwatch w = Stopwatch.StartNew();

                bool valid = ValidateOptions(rewriteOptions);
                if (!valid) { return FAILURE; }

                SarifLog actualLog = ReadSarifFile<SarifLog>(_fileSystem, rewriteOptions.InputFilePath);

                OptionallyEmittedData dataToInsert = rewriteOptions.DataToInsert.ToFlags();
                OptionallyEmittedData dataToRemove = rewriteOptions.DataToRemove.ToFlags();
                IDictionary<string, ArtifactLocation> originalUriBaseIds = rewriteOptions.ConstructUriBaseIdsDictionary();

                SarifLog reformattedLog = new RemoveOptionalDataVisitor(dataToRemove).VisitSarifLog(actualLog);
                reformattedLog = new InsertOptionalDataVisitor(dataToInsert, originalUriBaseIds).VisitSarifLog(reformattedLog);

                string fileName = CommandUtilities.GetTransformedOutputFileName(rewriteOptions);

                WriteSarifFile(_fileSystem, reformattedLog, fileName, rewriteOptions.Formatting);

                w.Stop();
                Console.WriteLine($"Rewrite completed in {w.Elapsed}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return FAILURE;
            }

            return SUCCESS;
        }

        private bool ValidateOptions(RewriteOptions rewriteOptions)
        {
            bool valid = true;

            valid &= rewriteOptions.Validate();

            valid &= DriverUtilities.ReportWhetherOutputFileCanBeCreated(rewriteOptions.OutputFilePath, rewriteOptions.Force, _fileSystem);

            return valid;
        }
    }
}