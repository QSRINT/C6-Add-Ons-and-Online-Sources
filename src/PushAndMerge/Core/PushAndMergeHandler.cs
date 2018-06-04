﻿using SwissAcademic.Citavi;
using SwissAcademic.Citavi.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SwissAcademic.Addons.PushAndMerge
{
    public static class PushAndMergeHandler
    {
        public static async Task ExecuteAsync(
            Form dialogOwner,
            Project sourceProject,
            Project targetProject, 
            PushAndMergeOptions options, 
            Progress<PercentageAndTextProgressInfo> progress, 
            CancellationToken cancellationToken = default(CancellationToken))
        {
            #region Selection
            var referenceSelectionSupporter = dialogOwner as ISupportReferenceSelection;

            IList<Reference> references = null;

            if (referenceSelectionSupporter == null)
            {
                references = sourceProject.References.ToList();
            }
            else
            {

                var referenceSelection = options.ReferenceSelection;

                if (referenceSelection == ReferenceSelection.Selected && referenceSelectionSupporter.IsAllSelected) referenceSelection = ReferenceSelection.Filter;
                if (referenceSelection == ReferenceSelection.Filter && !referenceSelectionSupporter.HasFilter) referenceSelection = ReferenceSelection.All;

                switch (referenceSelection)
                {
                    #region Filter

                    case ReferenceSelection.Filter:
                        references = referenceSelectionSupporter.GetFilteredReferences();
                        break;

                    #endregion

                    #region Selected

                    case ReferenceSelection.Selected:
                        references = referenceSelectionSupporter.GetSelectedReferences();
                        break;

                    #endregion

                    #region default

                    default:
                        {
                            references = sourceProject.References.ToList();
                        }
                        break;

                    #endregion
                }
            }
            #endregion

            targetProject.SuspendTrackingOfModificationInfo();

            try
            {
                var cloneOptions = new CitaviEntityCloneOptions
                {
                    CloneKnowledgeItemCategories = options.MergeKnowledgeItemCategories,
                    CloneKnowledgeItemKeywords = options.MergeKnowledgeItemKeywords,
                    CloneKnowledgeItemGroups = options.MergeKnowldgeItemGroups,
                    CloneReferenceAbstract = options.IncludeAbstract,
                    CloneReferenceCategories = options.IncludeCategories,
                    CloneReferenceCustomField1 = options.IncludeCustomField1,
                    CloneReferenceCustomField2 = options.IncludeCustomField2,
                    CloneReferenceCustomField3 = options.IncludeCustomField3,
                    CloneReferenceCustomField4 = options.IncludeCustomField4,
                    CloneReferenceCustomField5 = options.IncludeCustomField5,
                    CloneReferenceCustomField6 = options.IncludeCustomField6,
                    CloneReferenceCustomField7 = options.IncludeCustomField7,
                    CloneReferenceCustomField8 = options.IncludeCustomField8,
                    CloneReferenceCustomField9 = options.IncludeCustomField9,
                    CloneReferenceGroups = options.IncludeGroups,
                    CloneReferenceKeywords = options.IncludeKeywords,
                    CloneReferenceNotes = options.IncludeNotes,
                    CloneReferenceTableOfContents = options.IncludeTableOfContents,
                    MatchByReferenceIdentifier = options.MergeProjectOptions.HasFlag(MergeProjectOptions.EqualIdentifiers),
                    UpdateCreationAndModificationInfo = false
                };

                ClonePool.Reset();

                var clonesWithResult = references.CloneCollectionWithResults(targetProject, cloneOptions);
                var referencesToImport = new List<Reference>();
                var importGroupReferences = new List<Reference>();

                foreach (var r in clonesWithResult.Results)
                {
                    var reference = r.Clone as Reference;

                    if (reference == null) continue;

                    if (r.CloneResult == CloneResult.ReferenceIdentifierMatch)
                    {
                        MergeReferenceData((Reference)r.Source, reference, options);
                        await MergeKnowledgeItemsAsync((Reference)r.Source, reference, options);
                        importGroupReferences.Add(reference);
                        continue;
                    }

                    if (options.MergeProjectOptions.HasFlag(MergeProjectOptions.EqualStaticId))
                    {
                        foreach (var staticId in reference.StaticIds)
                        {
                            var matchedReference = targetProject.References.FindStaticId(staticId);

                            if (matchedReference != null)
                            {
                                MergeReferenceData(reference, matchedReference, options);
                                await MergeKnowledgeItemsAsync(reference, matchedReference, options);
                                importGroupReferences.Add(matchedReference);
                                continue;
                            }
                        }
                    }

                    if (options.MergeProjectOptions.HasFlag(MergeProjectOptions.EqualEssentialFields))
                    {
                        var matchedReference = targetProject.References.FirstOrDefault(
                            i => i.ReferenceType == reference.ReferenceType &&
                                 i.Authors.ContentEquals(reference.Authors, false) &&
                                 i.Title == reference.Title &&
                                 i.Subtitle == reference.Subtitle &&
                                 i.Year == reference.Year &&
                                 i.Edition == reference.Edition);

                        if (matchedReference != null)
                        {
                            MergeReferenceData(reference, matchedReference, options);
                            await MergeKnowledgeItemsAsync(reference, matchedReference, options);
                            importGroupReferences.Add(matchedReference);
                            continue;
                        }
                    }

                    if (options.CopyAllNonMatchedReferences)
                    {
                        referencesToImport.Add(reference);
                    }
                }
                targetProject.References.AddRange(referencesToImport);

                var concatedReferences = referencesToImport.Concat(importGroupReferences);

                if (concatedReferences.Any())
                {
                    var importGroup = new ImportGroup(targetProject, ImportGroupType.FileImport);
                    importGroup.Source = "Push&Merge";
                    importGroup.References.AddRange(referencesToImport.Concat(importGroupReferences));

                    targetProject.ImportGroups.Add(importGroup);

                    var shell = Program.ProjectShells.Single(i => i.Project == targetProject);
                    shell.PrimaryMainForm.ReferenceEditorFilterSet.Filters.ReplaceBy(new ReferenceFilter[] { new ReferenceFilter(importGroup) });
                }
            }
            finally
            {
                targetProject.ResumeTrackingOfModificationInfo();
            }
        }
        static void MergeReferenceData(Reference source, Reference target, PushAndMergeOptions options)
        {
            target.Abstract.Text = HandleReferenceMergeOptions(source.Abstract.Text, target.Abstract.Text, options.MergeReferenceOptionAbstract);
            target.Evaluation.Text = HandleReferenceMergeOptions(source.Evaluation.Text, target.Evaluation.Text, options.MergeReferenceOptionEvaluation);
            target.Notes = HandleReferenceMergeOptions(source.Notes, target.Notes, options.MergeReferenceOptionNotes);
            target.TableOfContents.Text = HandleReferenceMergeOptions(source.TableOfContents.Text, target.TableOfContents.Text, options.MergeReferenceOptionTableOfContents);

            switch(options.MergeReferenceOptionsCategories)
            {
                case MergeReferenceOptions.Ignore: break;
                case MergeReferenceOptions.Replace:
                    target.Categories.Clear();
                    target.Categories.AddRange(source.Categories.CloneCollection(target.Project));
                    break;
                case MergeReferenceOptions.Merge:
                    target.Categories.AddRange(source.Categories.CloneCollection(target.Project));
                    break;
            }
            switch(options.MergeReferenceOptionsKeywords)
            {
                case MergeReferenceOptions.Ignore: break;
                case MergeReferenceOptions.Replace:
                    target.Keywords.Clear();
                    target.Keywords.AddRange(source.Keywords.CloneCollection(target.Project));
                    break;
                case MergeReferenceOptions.Merge:
                    target.Keywords.AddRange(source.Keywords.CloneCollection(target.Project));
                    break;
            }
            switch (options.MergeReferenceOptionsGroups)
            {
                case MergeReferenceOptions.Ignore: break;
                case MergeReferenceOptions.Replace:
                    target.Groups.Clear();
                    target.Groups.AddRange(source.Groups.CloneCollection(target.Project));
                    break;
                case MergeReferenceOptions.Merge:
                    target.Groups.AddRange(source.Groups.CloneCollection(target.Project));
                    break;
            }
        }
        static async Task MergeKnowledgeItemsAsync(Reference source, Reference target, PushAndMergeOptions options)
        {
            ClonePool.Reset();
            var cloneOptions = new CitaviEntityCloneOptions
            {
                CloneKnowledgeItemCategories = options.MergeKnowledgeItemCategories,
                CloneKnowledgeItemKeywords = options.MergeKnowledgeItemKeywords,
                CloneKnowledgeItemGroups = options.MergeKnowldgeItemGroups,
                UpdateCreationAndModificationInfo = false,
                CreateNewId = true,
                MatchById = false
            };
            
            foreach (var quotation in source.Quotations)
            {
                var matchedKnowledgeItem = target.Quotations.FirstOrDefault(i => i.CreatedBy == quotation.CreatedBy && i.CreatedOn == quotation.CreatedOn);

                Location l = null;
                KnowledgeItem newQuotation = null;

                if (matchedKnowledgeItem != null)
                {
                    if (options.IgnoreKnowledgeItemOnMatch) continue;

                    newQuotation = quotation.Clone(target, cloneOptions);
                }
                else
                {
                    newQuotation = quotation.Clone(target, cloneOptions);
                }


                newQuotation = target.Quotations.Add(newQuotation);

                var quotationAnnotations = quotation.EntityLinks.Where(i => i.Target is Annotation).Select(i => i.Target as Annotation);

                if (quotationAnnotations.Any())
                {
                    if (!target.Locations.Any(
                        i => i.Address?.LinkedResourceType == LinkedResourceType.AbsoluteFileUri || 
                        i.Address?.LinkedResourceType == LinkedResourceType.RelativeFileUri || 
                        i.Address?.LinkedResourceType == LinkedResourceType.AttachmentFile || 
                        i.Address?.LinkedResourceType == LinkedResourceType.AttachmentRemote))
                    {
                        l = quotationAnnotations.First().Location.Clone(newQuotation.Reference);
                        l.Annotations.Clear();

                        l = target.Locations.Add(l);
                    }
                    else
                    {
                        l = await quotationAnnotations.First().Location.TryFindEqualFileLocation(newQuotation.Reference.Locations);
                    }

                    ConnectAnnotations(newQuotation, l, quotationAnnotations);
                }
            }
       
        }
        static string HandleReferenceMergeOptions(string source, string target, MergeReferenceContentOptions options)
        {
            switch(options)
            {
                case MergeReferenceContentOptions.Complete:
                    return $"{target}{System.Environment.NewLine}---{System.Environment.NewLine}{source}";
                case MergeReferenceContentOptions.CompleteIfEmpty:
                    return string.IsNullOrEmpty(target) ? source : target;
                case MergeReferenceContentOptions.CompleIfNotEqual:
                    return target.Equals(source, StringComparison.InvariantCultureIgnoreCase) ? target : $"{target}{System.Environment.NewLine}---{System.Environment.NewLine}{source}";
                case MergeReferenceContentOptions.Ignore:
                    return target;
                case MergeReferenceContentOptions.Override:
                    return source;
                default: throw new InvalidOperationException();
            }
        }
        static void ConnectAnnotations(KnowledgeItem quotation, Location location, IEnumerable<Annotation> annotations)
        {
            ClonePool.Reset();
            foreach (var annotation in annotations)
            {
                var newAnnotation = annotation.Clone(location);
                newAnnotation = location.Annotations.Add(newAnnotation);

                location.Project.EntityLinks.Add(quotation, newAnnotation, EntityLink.PdfKnowledgeItemIndication);
            }
        }
    }
}
