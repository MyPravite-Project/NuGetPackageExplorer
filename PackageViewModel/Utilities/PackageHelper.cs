﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NuGet;
using NuGetPackageExplorer.Types;

namespace PackageExplorerViewModel
{
    internal static class PackageHelper
    {
        [SuppressMessage(
            "Microsoft.Design",
            "CA1031:DoNotCatchGeneralExceptionTypes",
            Justification = "We don't really care of deleting temp file fails.")]
        public static void SavePackage(IPackageMetadata packageMetadata, IEnumerable<IPackageFile> files,
                                       string targetFilePath, bool useTempFile)
        {
            var builder = new PackageBuilder();
            // set metadata
            CopyMetadata(packageMetadata, builder);
            // add files
            builder.Files.AddRange(files);

            // create package in the temprary file first in case the operation fails which would
            // override existing file with a 0-byte file.
            string fileNameToUse = useTempFile ? Path.GetTempFileName() : targetFilePath;
            try
            {
                using (Stream stream = File.Create(fileNameToUse))
                {
                    builder.Save(stream);
                }

                if (useTempFile)
                {
                    File.Copy(fileNameToUse, targetFilePath, true);
                }
            }
            finally
            {
                try
                {
                    if (useTempFile && File.Exists(fileNameToUse))
                    {
                        File.Delete(fileNameToUse);
                    }
                }
                catch
                {
                    // don't care if this fails
                }
            }
        }

        private static void CopyMetadata(IPackageMetadata source, PackageBuilder builder)
        {
            builder.Id = source.Id;
            builder.Version = source.Version;
            builder.Title = source.Title;
            builder.Authors.AddRange(source.Authors);
            builder.Owners.AddRange(source.Owners);
            builder.IconUrl = source.IconUrl;
            builder.LicenseUrl = source.LicenseUrl;
            builder.ProjectUrl = source.ProjectUrl;
            builder.RequireLicenseAcceptance = source.RequireLicenseAcceptance;
            builder.Serviceable = source.Serviceable;
            builder.DevelopmentDependency = source.DevelopmentDependency;
            builder.Description = source.Description;
            builder.Summary = source.Summary;
            builder.ReleaseNotes = source.ReleaseNotes;
            builder.Copyright = source.Copyright;
            builder.Language = source.Language;
            builder.Tags.AddRange(ParseTags(source.Tags));
            builder.DependencySets.AddRange(source.DependencySets);
            builder.FrameworkReferences.AddRange(source.FrameworkAssemblies);
            builder.PackageAssemblyReferences.AddRange(source.PackageAssemblyReferences);
            builder.MinClientVersion = source.MinClientVersion;
        }

        public static IPackage BuildPackage(IPackageMetadata metadata, IEnumerable<IPackageFile> files)
        {
            var builder = new PackageBuilder();
            CopyMetadata(metadata, builder);
            builder.Files.AddRange(files);
            return builder.Build();
        }

        public static IEnumerable<PackageIssue> Validate
            (this IPackage package, IEnumerable<IPackageRule> rules, string packageSource)
        {
            foreach (IPackageRule rule in rules)
            {
                if (rule != null)
                {
                    PackageIssue[] issues = null;
                    try
                    {
                        issues = rule.Validate(package, packageSource).ToArray();
                    }
                    catch (Exception)
                    {
                        issues = new PackageIssue[0];
                    }

                    // can't yield inside a try/catch block
                    foreach (PackageIssue issue in issues)
                    {
                        yield return issue;
                    }
                }
            }
        }

        /// <summary>
        /// Tags come in this format. tag1 tag2 tag3 etc..
        /// </summary>
        private static IEnumerable<string> ParseTags(string tags)
        {
            if (tags == null)
            {
                return Enumerable.Empty<string>();
            }
            return tags.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}