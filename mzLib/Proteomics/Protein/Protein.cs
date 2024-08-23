﻿using Proteomics.ProteolyticDigestion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Omics;
using Omics.Digestion;
using Omics.Fragmentation;
using Omics.Modifications;
using MzLibUtil;
using Easy.Common.Extensions;

namespace Proteomics
{
    public class Protein : IBioPolymer
    {
        private List<ProteolysisProduct> _proteolysisProducts;

        /// <summary>
        /// Protein. Filters out modifications that do not match their amino acid target site.
        /// </summary>
        /// <param name="sequence">Base sequence of the protein.</param>
        /// <param name="accession">Unique accession for the protein.</param>
        /// <param name="organism">Organism with this protein.</param>
        /// <param name="geneNames">List of gene names as tuple of (nameType, name), e.g. (primary, HLA-A)</param>
        /// <param name="oneBasedModifications">Modifications at positions along the sequence.</param>
        /// <param name="proteolysisProducts"></param>
        /// <param name="name"></param>
        /// <param name="fullName"></param>
        /// <param name="isDecoy"></param>
        /// <param name="isContaminant"></param>
        /// <param name="databaseReferences"></param>
        /// <param name="sequenceVariations"></param>
        /// <param name="disulfideBonds"></param>
        /// <param name="spliceSites"></param>
        /// <param name="databaseFilePath"></param>
        public Protein(string sequence, string accession, string organism = null, List<Tuple<string, string>> geneNames = null,
            IDictionary<int, List<Modification>> oneBasedModifications = null, List<ProteolysisProduct> proteolysisProducts = null,
            string name = null, string fullName = null, bool isDecoy = false, bool isContaminant = false, List<DatabaseReference> databaseReferences = null,
            List<SequenceVariation> sequenceVariations = null, List<SequenceVariation> appliedSequenceVariations = null, string sampleNameForVariants = null,
            List<DisulfideBond> disulfideBonds = null, List<SpliceSite> spliceSites = null, string databaseFilePath = null, bool addTruncations = false)
        {
            // Mandatory
            BaseSequence = sequence;
            NonVariantProtein = this;
            Accession = accession;

            Name = name;
            Organism = organism;
            FullName = fullName;
            IsDecoy = isDecoy;
            IsContaminant = isContaminant;
            DatabaseFilePath = databaseFilePath;
            SampleNameForVariants = sampleNameForVariants;

            GeneNames = geneNames ?? new List<Tuple<string, string>>();
            _proteolysisProducts = proteolysisProducts ?? new List<ProteolysisProduct>();
            SequenceVariations = sequenceVariations ?? new List<SequenceVariation>();
            AppliedSequenceVariations = appliedSequenceVariations ?? new List<SequenceVariation>();
            OriginalNonVariantModifications = oneBasedModifications ?? new Dictionary<int, List<Modification>>();
            if (oneBasedModifications != null)
            {
                OneBasedPossibleLocalizedModifications = SelectValidOneBaseMods(oneBasedModifications);
            }
            else
            {
                OneBasedPossibleLocalizedModifications = new Dictionary<int, List<Modification>>();
            }
            DatabaseReferences = databaseReferences ?? new List<DatabaseReference>();
            DisulfideBonds = disulfideBonds ?? new List<DisulfideBond>();
            SpliceSites = spliceSites ?? new List<SpliceSite>();

            if (addTruncations)
            {
                this.AddTruncations();
            }
        }

        /// <summary>
        /// Protein construction that clones a protein but assigns a different base sequence
        /// For use in SILAC experiments and in decoy construction
        /// </summary>
        /// <param name="originalProtein"></param>
        /// <param name="newBaseSequence"></param>
        /// <param name="silacAccession"></param>
        public Protein(Protein originalProtein, string newBaseSequence)
        {
            BaseSequence = newBaseSequence;
            Accession = originalProtein.Accession;
            NonVariantProtein = originalProtein.NonVariantProtein;
            Name = originalProtein.Name;
            Organism = originalProtein.Organism;
            FullName = originalProtein.FullName;
            IsDecoy = originalProtein.IsDecoy;
            IsContaminant = originalProtein.IsContaminant;
            DatabaseFilePath = originalProtein.DatabaseFilePath;
            SampleNameForVariants = originalProtein.SampleNameForVariants;
            GeneNames = originalProtein.GeneNames;
            _proteolysisProducts = originalProtein._proteolysisProducts;
            SequenceVariations = originalProtein.SequenceVariations;
            AppliedSequenceVariations = originalProtein.AppliedSequenceVariations;
            OriginalNonVariantModifications = originalProtein.OriginalNonVariantModifications;
            OneBasedPossibleLocalizedModifications = originalProtein.OneBasedPossibleLocalizedModifications;
            DatabaseReferences = originalProtein.DatabaseReferences;
            DisulfideBonds = originalProtein.DisulfideBonds;
            SpliceSites = originalProtein.SpliceSites;
            DatabaseFilePath = originalProtein.DatabaseFilePath;
        }

        /// <summary>
        /// Protein construction with applied variations
        /// </summary>
        /// <param name="variantBaseSequence"></param>
        /// <param name="protein"></param>
        /// <param name="appliedSequenceVariations"></param>
        /// <param name="applicableProteolysisProducts"></param>
        /// <param name="oneBasedModifications"></param>
        /// <param name="sampleNameForVariants"></param>
        public Protein(string variantBaseSequence, Protein protein, IEnumerable<SequenceVariation> appliedSequenceVariations,
            IEnumerable<ProteolysisProduct> applicableProteolysisProducts, IDictionary<int, List<Modification>> oneBasedModifications, string sampleNameForVariants)
            : this(variantBaseSequence,
                  VariantApplication.GetAccession(protein, appliedSequenceVariations),
                  organism: protein.Organism,
                  geneNames: new List<Tuple<string, string>>(protein.GeneNames),
                  oneBasedModifications: oneBasedModifications != null ? oneBasedModifications.ToDictionary(x => x.Key, x => x.Value) : new Dictionary<int, List<Modification>>(),
                  proteolysisProducts: new List<ProteolysisProduct>(applicableProteolysisProducts ?? new List<ProteolysisProduct>()),
                  name: GetName(appliedSequenceVariations, protein.Name),
                  fullName: GetName(appliedSequenceVariations, protein.FullName),
                  isDecoy: protein.IsDecoy,
                  isContaminant: protein.IsContaminant,
                  databaseReferences: new List<DatabaseReference>(protein.DatabaseReferences),
                  sequenceVariations: new List<SequenceVariation>(protein.SequenceVariations),
                  disulfideBonds: new List<DisulfideBond>(protein.DisulfideBonds),
                  spliceSites: new List<SpliceSite>(protein.SpliceSites),
                  databaseFilePath: protein.DatabaseFilePath)
        {
            NonVariantProtein = protein.NonVariantProtein;
            OriginalNonVariantModifications = NonVariantProtein.OriginalNonVariantModifications;
            AppliedSequenceVariations = (appliedSequenceVariations ?? new List<SequenceVariation>()).ToList();
            SampleNameForVariants = sampleNameForVariants;
        }

        /// <summary>
        /// Modifications (values) located at one-based protein positions (keys)
        /// </summary>
        public IDictionary<int, List<Modification>> OneBasedPossibleLocalizedModifications { get; private set; }

        /// <summary>
        /// The list of gene names consists of tuples, where Item1 is the type of gene name, and Item2 is the name. There may be many genes and names of a certain type produced when reading an XML protein database.
        /// </summary>
        public IEnumerable<Tuple<string, string>> GeneNames { get; }

        /// <summary>
        /// Unique accession for this protein.
        /// </summary>
        public string Accession { get; }

        /// <summary>
        /// Base sequence, which may contain applied sequence variations.
        /// </summary>
        public string BaseSequence { get; private set; }

        public string Organism { get; }
        public bool IsDecoy { get; }
        public IEnumerable<SequenceVariation> SequenceVariations { get; }
        public IEnumerable<DisulfideBond> DisulfideBonds { get; }
        public IEnumerable<SpliceSite> SpliceSites { get; }

        //TODO: Generate all the proteolytic products as distinct proteins during XML reading and delete the ProteolysisProducts parameter
        public IEnumerable<ProteolysisProduct> ProteolysisProducts
        { get { return _proteolysisProducts; } }

        public IEnumerable<DatabaseReference> DatabaseReferences { get; }
        public string DatabaseFilePath { get; }

        /// <summary>
        /// Protein before applying variations.
        /// </summary>
        public Protein NonVariantProtein { get; }

        /// <summary>
        /// Sequence variations that have been applied to the base sequence.
        /// </summary>
        public List<SequenceVariation> AppliedSequenceVariations { get; }

        /// <summary>
        /// Sample name from which applied variants came, e.g. tumor or normal.
        /// </summary>
        public string SampleNameForVariants { get; }

        public double Probability { get; set; } // for protein pep project

        public int Length => BaseSequence.Length;

        public string FullDescription => Accession + "|" + Name + "|" + FullName;

        public string Name { get; }
        public string FullName { get; }
        public bool IsContaminant { get; }
        internal IDictionary<int, List<Modification>> OriginalNonVariantModifications { get; set; }
        public char this[int zeroBasedIndex] => BaseSequence[zeroBasedIndex];

        /// <summary>
        /// Formats a string for a UniProt fasta header. See https://www.uniprot.org/help/fasta-headers.
        /// Note that the db field isn't very applicable here, so mz is placed in to denote written by mzLib.
        /// </summary>
        public string GetUniProtFastaHeader()
        {
            var n = GeneNames.FirstOrDefault();
            string geneName = n == null ? "" : n.Item2;

            return string.Format("mz|{0}|{1} {2} OS={3} GN={4}", Accession, Name, FullName, Organism, geneName);
        }

        /// <summary>
        /// Formats a string for an ensembl header
        /// </summary>
        public string GetEnsemblFastaHeader()
        {
            return string.Format("{0} {1}", Accession, FullName);
        }

        /// <summary>
        /// Gets peptides for digestion of a protein
        /// Legacy
        /// </summary>
        public IEnumerable<PeptideWithSetModifications> Digest(DigestionParams digestionParams,
            List<Modification> allKnownFixedModifications, List<Modification> variableModifications,
            List<SilacLabel> silacLabels = null, (SilacLabel startLabel, SilacLabel endLabel)? turnoverLabels = null,
            bool topDownTruncationSearch = false) =>
            Digest((IDigestionParams)digestionParams, allKnownFixedModifications, variableModifications, silacLabels, turnoverLabels, topDownTruncationSearch)
                .Cast<PeptideWithSetModifications>();

        /// <summary>
        /// Gets peptides for digestion of a protein
        /// Implemented with interfaces to allow for use of both Proteomics and Omics classes
        /// </summary>
        public IEnumerable<IBioPolymerWithSetMods> Digest(IDigestionParams digestionParams, List<Modification> allKnownFixedModifications,
            List<Modification> variableModifications, List<SilacLabel> silacLabels = null, (SilacLabel startLabel, SilacLabel endLabel)? turnoverLabels = null, bool topDownTruncationSearch = false)
        {

            if (digestionParams is not DigestionParams digestionParameters)
                throw new ArgumentException(
                    "DigestionParameters must be of type DigestionParams for protein digestion");


            //can't be null
            allKnownFixedModifications = allKnownFixedModifications ?? new List<Modification>();
            // add in any modifications that are caused by protease digestion
            if (digestionParameters.Protease.CleavageMod != null && !allKnownFixedModifications.Contains(digestionParameters.Protease.CleavageMod))
            {
                allKnownFixedModifications.Add(digestionParameters.Protease.CleavageMod);
            }
            variableModifications = variableModifications ?? new List<Modification>();
            CleavageSpecificity searchModeType = digestionParameters.SearchModeType;

            ProteinDigestion digestion = new(digestionParameters, allKnownFixedModifications, variableModifications);
            IEnumerable<ProteolyticPeptide> unmodifiedPeptides =
                searchModeType == CleavageSpecificity.Semi ?
                digestion.SpeedySemiSpecificDigestion(this) :
                    digestion.Digestion(this, topDownTruncationSearch);

            if (digestionParameters.KeepNGlycopeptide || digestionParameters.KeepOGlycopeptide)
            {
                unmodifiedPeptides = GetGlycoPeptides(unmodifiedPeptides, digestionParameters.KeepNGlycopeptide, digestionParameters.KeepOGlycopeptide);
            }

            IEnumerable<PeptideWithSetModifications> modifiedPeptides = unmodifiedPeptides.SelectMany(peptide => 
                peptide.GetModifiedPeptides(allKnownFixedModifications, digestionParameters, variableModifications));

            //Remove terminal modifications (if needed)
            if (searchModeType == CleavageSpecificity.SingleN ||
                searchModeType == CleavageSpecificity.SingleC ||
                (searchModeType == CleavageSpecificity.None && (digestionParams.FragmentationTerminus == FragmentationTerminus.N || digestionParams.FragmentationTerminus == FragmentationTerminus.C)))
            {
                modifiedPeptides = RemoveTerminalModifications(modifiedPeptides, digestionParams.FragmentationTerminus, allKnownFixedModifications);
            }

            //add silac labels (if needed)
            if (silacLabels != null)
            {
                return GetSilacPeptides(modifiedPeptides, silacLabels, digestionParameters.GeneratehUnlabeledProteinsForSilac, turnoverLabels);
            }

            return modifiedPeptides;
        }

        /// <summary>
        /// Remove terminal modifications from the C-terminus of SingleN peptides and the N-terminus of SingleC peptides/
        /// These terminal modifications create redundant entries and increase search time
        /// </summary>
        internal static IEnumerable<PeptideWithSetModifications> RemoveTerminalModifications(IEnumerable<PeptideWithSetModifications> modifiedPeptides, FragmentationTerminus fragmentationTerminus, IEnumerable<Modification> allFixedMods)
        {
            string terminalStringToLookFor = fragmentationTerminus == FragmentationTerminus.N ? "C-terminal" : "N-terminal";
            List<Modification> fixedTerminalMods = allFixedMods.Where(x => x.LocationRestriction.Contains(terminalStringToLookFor)).ToList();
            foreach (PeptideWithSetModifications pwsm in modifiedPeptides)
            {
                if (!pwsm.AllModsOneIsNterminus.Values.Any(x => x.LocationRestriction.Contains(terminalStringToLookFor) && !fixedTerminalMods.Contains(x)))
                {
                    yield return pwsm;
                }
            }
        }

        /// <summary>
        /// Add additional peptides with SILAC amino acids
        /// </summary>
        internal IEnumerable<PeptideWithSetModifications> GetSilacPeptides(IEnumerable<PeptideWithSetModifications> originalPeptides, List<SilacLabel> silacLabels, bool generateUnlabeledProteins, (SilacLabel startLabel, SilacLabel endLabel)? turnoverLabels)
        {
            //if this is a multiplex experiment (pooling multiple samples, not a turnover), then only create the fully unlabeled/labeled peptides
            if (turnoverLabels == null)
            {
                //unlabeled peptides
                if (generateUnlabeledProteins)
                {
                    foreach (PeptideWithSetModifications pwsm in originalPeptides)
                    {
                        yield return pwsm;
                    }
                }

                //fully labeled peptides
                foreach (SilacLabel label in silacLabels)
                {
                    Protein silacProtein = GenerateFullyLabeledSilacProtein(label);
                    foreach (PeptideWithSetModifications pwsm in originalPeptides)
                    {
                        //duplicate the peptides with the updated protein sequence that contains only silac labels
                        yield return new PeptideWithSetModifications(silacProtein, pwsm.DigestionParams, pwsm.OneBasedStartResidueInProtein, pwsm.OneBasedEndResidueInProtein, pwsm.CleavageSpecificityForFdrCategory, pwsm.PeptideDescription, pwsm.MissedCleavages, pwsm.AllModsOneIsNterminus, pwsm.NumFixedMods);
                    }
                }
            }
            else //if this is a turnover experiment, we want to be able to look for peptides containing mixtures of heavy and light amino acids (typically occurs for missed cleavages)
            {
                (SilacLabel startLabel, SilacLabel endLabel) turnoverLabelsValue = turnoverLabels.Value;
                SilacLabel startLabel = turnoverLabelsValue.startLabel;
                SilacLabel endLabel = turnoverLabelsValue.endLabel;

                //This allows you to move from one label to another (rather than unlabeled->labeled or labeled->unlabeled). Useful for when your lab is swimming in cash and you have stock in a SILAC company
                if (startLabel != null && endLabel != null) //if neither the start nor end conditions are unlabeled, then generate fully labeled proteins using the "startLabel" (otherwise maintain the unlabeled)
                {
                    Protein silacStartProtein = GenerateFullyLabeledSilacProtein(startLabel);
                    PeptideWithSetModifications[] originalPeptideArray = originalPeptides.ToArray();
                    for (int i = 0; i < originalPeptideArray.Length; i++)
                    {
                        PeptideWithSetModifications pwsm = originalPeptideArray[i];
                        //duplicate the peptides with the updated protein sequence that contains only silac labels
                        originalPeptideArray[i] = new PeptideWithSetModifications(silacStartProtein, pwsm.DigestionParams, pwsm.OneBasedStartResidueInProtein, pwsm.OneBasedEndResidueInProtein, pwsm.CleavageSpecificityForFdrCategory, pwsm.PeptideDescription, pwsm.MissedCleavages, pwsm.AllModsOneIsNterminus, pwsm.NumFixedMods);
                    }
                    originalPeptides = originalPeptideArray;

                    //modify the end label amino acids to recognize the new "original" amino acid
                    //get the residues that were changed
                    List<SilacLabel> originalLabels = new List<SilacLabel> { startLabel };
                    if (startLabel.AdditionalLabels != null)
                    {
                        originalLabels.AddRange(startLabel.AdditionalLabels);
                    }
                    SilacLabel startLabelWithSharedOriginalAminoAcid = originalLabels.Where(x => x.OriginalAminoAcid == endLabel.OriginalAminoAcid).FirstOrDefault();
                    SilacLabel updatedEndLabel = startLabelWithSharedOriginalAminoAcid == null ?
                        endLabel :
                        new SilacLabel(startLabelWithSharedOriginalAminoAcid.AminoAcidLabel, endLabel.AminoAcidLabel, endLabel.LabelChemicalFormula, endLabel.ConvertMassDifferenceToDouble());
                    if (endLabel.AdditionalLabels != null)
                    {
                        foreach (SilacLabel additionalLabel in endLabel.AdditionalLabels)
                        {
                            startLabelWithSharedOriginalAminoAcid = originalLabels.Where(x => x.OriginalAminoAcid == additionalLabel.OriginalAminoAcid).FirstOrDefault();
                            updatedEndLabel.AddAdditionalSilacLabel(
                                startLabelWithSharedOriginalAminoAcid == null ?
                                additionalLabel :
                                new SilacLabel(startLabelWithSharedOriginalAminoAcid.AminoAcidLabel, additionalLabel.AminoAcidLabel, additionalLabel.LabelChemicalFormula, additionalLabel.ConvertMassDifferenceToDouble()));
                        }
                    }

                    //double check that all labeled amino acids can become unlabeled/relabeled
                    if (startLabel.AdditionalLabels != null)
                    {
                        foreach (SilacLabel originalLabel in originalLabels)
                        {
                            if (updatedEndLabel.OriginalAminoAcid != originalLabel.AminoAcidLabel &&
                                (updatedEndLabel.AdditionalLabels == null || !updatedEndLabel.AdditionalLabels.Any(x => x.OriginalAminoAcid == originalLabel.AminoAcidLabel)))
                            {
                                updatedEndLabel.AddAdditionalSilacLabel(new SilacLabel(originalLabel.AminoAcidLabel, originalLabel.OriginalAminoAcid, originalLabel.LabelChemicalFormula, originalLabel.ConvertMassDifferenceToDouble()));
                            }
                        }
                    }
                    endLabel = updatedEndLabel;
                }

                //add all unlabeled (or if no unlabeled, then the startLabeled) peptides
                foreach (PeptideWithSetModifications pwsm in originalPeptides)
                {
                    yield return pwsm;
                }

                //the order (below) matters when neither labels are null, because the fully labeled "start" has already been created above, so we want to use the end label here if it's not unlabeled (null)
                SilacLabel label = endLabel ?? startLabel; //pick the labeled (not the unlabeled). If no unlabeled, take the endLabel

                Protein silacEndProtein = GenerateFullyLabeledSilacProtein(label);

                //add all peptides containing any label (may also contain unlabeled)
                if (label.AdditionalLabels == null) //if there's only one (which is common)
                {
                    //get the residues to change
                    char originalResidue = label.OriginalAminoAcid;
                    char labeledResidue = label.AminoAcidLabel;

                    //label peptides
                    foreach (PeptideWithSetModifications pwsm in originalPeptides)
                    {
                        //find the indexes in the base sequence for labeling
                        char[] baseSequenceArray = pwsm.BaseSequence.ToArray();
                        List<int> indexesOfResiduesToBeLabeled = new List<int>();
                        for (int c = 0; c < baseSequenceArray.Length; c++)
                        {
                            if (baseSequenceArray[c] == originalResidue)
                            {
                                indexesOfResiduesToBeLabeled.Add(c);
                            }
                        }
                        //if there's something to label
                        if (indexesOfResiduesToBeLabeled.Count != 0)
                        {
                            List<PeptideWithSetModifications> pwsmsForCombinatorics = new List<PeptideWithSetModifications> { pwsm };
                            for (int a = 0; a < indexesOfResiduesToBeLabeled.Count; a++)
                            {
                                List<PeptideWithSetModifications> localPwsmsForCombinatorics = new List<PeptideWithSetModifications>();
                                foreach (PeptideWithSetModifications pwsmCombination in pwsmsForCombinatorics)
                                {
                                    char[] combinatoricBaseSequenceArray = pwsmCombination.BaseSequence.ToArray();
                                    combinatoricBaseSequenceArray[indexesOfResiduesToBeLabeled[a]] = labeledResidue;
                                    string updatedBaseSequence = string.Concat(combinatoricBaseSequenceArray);

                                    PeptideWithSetModifications labeledPwsm = new PeptideWithSetModifications(silacEndProtein, pwsm.DigestionParams,
                                        pwsm.OneBasedStartResidueInProtein, pwsm.OneBasedEndResidueInProtein, pwsm.CleavageSpecificityForFdrCategory,
                                        pwsm.PeptideDescription, pwsm.MissedCleavages, pwsm.AllModsOneIsNterminus, pwsm.NumFixedMods, updatedBaseSequence);
                                    yield return labeledPwsm; //return
                                    localPwsmsForCombinatorics.Add(labeledPwsm); //add so it can be used again
                                }
                                pwsmsForCombinatorics.AddRange(localPwsmsForCombinatorics);
                            }
                        }
                    }
                }
                else //if there are more than one (i.e. K and R are labeled)
                {
                    //get the residues to change
                    char[] originalResidues = new char[label.AdditionalLabels.Count + 1];
                    char[] labeledResidues = new char[label.AdditionalLabels.Count + 1];
                    originalResidues[0] = label.OriginalAminoAcid;
                    labeledResidues[0] = label.AminoAcidLabel;
                    for (int i = 0; i < label.AdditionalLabels.Count; i++)
                    {
                        originalResidues[i + 1] = label.AdditionalLabels[i].OriginalAminoAcid;
                        labeledResidues[i + 1] = label.AdditionalLabels[i].AminoAcidLabel;
                    }

                    //label peptides
                    foreach (PeptideWithSetModifications pwsm in originalPeptides)
                    {
                        //find the indexes in the base sequence for labeling
                        char[] baseSequenceArray = pwsm.BaseSequence.ToArray();
                        Dictionary<int, char> indexesOfResiduesToBeLabeled = new Dictionary<int, char>();
                        for (int peptideResidueIndex = 0; peptideResidueIndex < baseSequenceArray.Length; peptideResidueIndex++)
                        {
                            for (int silacResidue = 0; silacResidue < originalResidues.Length; silacResidue++)
                            {
                                if (baseSequenceArray[peptideResidueIndex] == originalResidues[silacResidue])
                                {
                                    indexesOfResiduesToBeLabeled.Add(peptideResidueIndex, labeledResidues[silacResidue]);
                                }
                            }
                        }
                        //if there's something to label
                        if (indexesOfResiduesToBeLabeled.Count != 0)
                        {
                            List<PeptideWithSetModifications> pwsmsForCombinatorics = new List<PeptideWithSetModifications> { pwsm };
                            foreach (KeyValuePair<int, char> kvp in indexesOfResiduesToBeLabeled)
                            {
                                List<PeptideWithSetModifications> localPwsmsForCombinatorics = new List<PeptideWithSetModifications>();
                                foreach (PeptideWithSetModifications pwsmCombination in pwsmsForCombinatorics)
                                {
                                    char[] combinatoricBaseSequenceArray = pwsmCombination.BaseSequence.ToArray();
                                    combinatoricBaseSequenceArray[kvp.Key] = kvp.Value;
                                    string updatedBaseSequence = string.Concat(combinatoricBaseSequenceArray);

                                    PeptideWithSetModifications labeledPwsm = new PeptideWithSetModifications(silacEndProtein, pwsm.DigestionParams,
                                        pwsm.OneBasedStartResidueInProtein, pwsm.OneBasedEndResidueInProtein, pwsm.CleavageSpecificityForFdrCategory,
                                        pwsm.PeptideDescription, pwsm.MissedCleavages, pwsm.AllModsOneIsNterminus, pwsm.NumFixedMods, updatedBaseSequence);
                                    yield return labeledPwsm; //return
                                    localPwsmsForCombinatorics.Add(labeledPwsm); //add so it can be used again
                                }
                                pwsmsForCombinatorics.AddRange(localPwsmsForCombinatorics);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Only keep glycopeptides by filtering the NGlycopeptide motif 'NxS || NxT' or OGlycopeptide motif 'S || T'
        /// </summary>
        internal IEnumerable<ProteolyticPeptide> GetGlycoPeptides(IEnumerable<ProteolyticPeptide> originalPeptides, bool keepNGlycopeptide, bool keepOGlycopeptide)
        {
            Regex rgx = new Regex("N[A-Z][ST]");
            foreach (ProteolyticPeptide pwsm in originalPeptides)
            {
                bool yielded = false;
                if (keepNGlycopeptide)
                {
                    if (rgx.IsMatch(pwsm.BaseSequence))
                    {
                        yielded = true;
                        yield return pwsm;
                    }
                }

                if (keepOGlycopeptide && !yielded)
                {
                    if (pwsm.BaseSequence.Contains('S') || pwsm.BaseSequence.Contains('T'))
                    {
                        yield return pwsm;
                    }
                }
            }
        }

        /// <summary>
        /// Generates a protein that is fully labeled with the specified silac label
        /// </summary>
        private Protein GenerateFullyLabeledSilacProtein(SilacLabel label)
        {
            string updatedBaseSequence = BaseSequence.Replace(label.OriginalAminoAcid, label.AminoAcidLabel);
            if (label.AdditionalLabels != null) //if there is more than one label per replicate (i.e both R and K were labeled in a sample before pooling)
            {
                foreach (SilacLabel additionalLabel in label.AdditionalLabels)
                {
                    updatedBaseSequence = updatedBaseSequence.Replace(additionalLabel.OriginalAminoAcid, additionalLabel.AminoAcidLabel);
                }
            }
            return new Protein(this, updatedBaseSequence);
        }

        /// <summary>
        /// Gets proteins with applied variants from this protein
        /// </summary>
        public List<Protein> GetVariantProteins(int maxAllowedVariantsForCombinitorics = 4, int minAlleleDepth = 1)
        {
            return VariantApplication.ApplyVariants(this, SequenceVariations, maxAllowedVariantsForCombinitorics, minAlleleDepth);
        }

        /// <summary>
        /// Restore all modifications that were read in, including those that did not match their target amino acid.
        /// </summary>
        public void RestoreUnfilteredModifications()
        {
            OneBasedPossibleLocalizedModifications = OriginalNonVariantModifications;
        }

        /// <summary>
        /// Filters modifications that do not match their target amino acid.
        /// </summary>
        /// <param name="dict"></param>
        /// <returns></returns>
        private IDictionary<int, List<Modification>> SelectValidOneBaseMods(IDictionary<int, List<Modification>> dict)
        {
            Dictionary<int, List<Modification>> validModDictionary = new Dictionary<int, List<Modification>>();
            foreach (KeyValuePair<int, List<Modification>> entry in dict)
            {
                List<Modification> validMods = new List<Modification>();
                foreach (Modification m in entry.Value)
                {
                    //mod must be valid mod and the motif of the mod must be present in the protein at the specified location
                    if (m.ValidModification && ModificationLocalization.ModFits(m, BaseSequence, 0, BaseSequence.Length, entry.Key))
                    {
                        validMods.Add(m);
                    }
                }

                if (validMods.Any())
                {
                    if (validModDictionary.Keys.Contains(entry.Key))
                    {
                        validModDictionary[entry.Key].AddRange(validMods);
                    }
                    else
                    {
                        validModDictionary.Add(entry.Key, validMods);
                    }
                }
            }
            return validModDictionary;
        }
        /// <summary>
        /// Protein XML files contain annotated proteolysis products for many proteins (e.g. signal peptides, chain peptides).
        /// This method adds N- and C-terminal truncations to these products.
        /// </summary>

        public void AddTruncationsToExistingProteolysisProducts(int fullProteinOneBasedBegin, int fullProteinOneBasedEnd, bool addNterminalDigestionTruncations, bool addCterminalDigestionTruncations, int minProductBaseSequenceLength, int lengthOfProteolysis, string proteolyisisProductName)
        {
            bool sequenceContainsNterminus = (fullProteinOneBasedBegin == 1);

            if (sequenceContainsNterminus)
            {
                //Digest N-terminus
                if (addNterminalDigestionTruncations)
                {
                    if (BaseSequence.Substring(0, 1) == "M")
                    {
                        AddNterminalTruncations(lengthOfProteolysis + 1, fullProteinOneBasedBegin, fullProteinOneBasedEnd, minProductBaseSequenceLength, proteolyisisProductName);
                    }
                    else
                    {
                        AddNterminalTruncations(lengthOfProteolysis, fullProteinOneBasedBegin, fullProteinOneBasedEnd, minProductBaseSequenceLength, proteolyisisProductName);
                    }
                }
                //Digest C-terminus -- not effected by variable N-terminus behavior
                if (addCterminalDigestionTruncations)
                {
                    // if first residue is M, then we have to add c-terminal markers for both with and without the M
                    if (BaseSequence.Substring(0, 1) == "M")
                    {
                        //add sequences WITHOUT methionine
                        AddCterminalTruncations(lengthOfProteolysis, fullProteinOneBasedEnd, fullProteinOneBasedBegin + 1, minProductBaseSequenceLength, proteolyisisProductName);
                    }
                    //add sequences with methionine
                    AddCterminalTruncations(lengthOfProteolysis, fullProteinOneBasedEnd, fullProteinOneBasedBegin, minProductBaseSequenceLength, proteolyisisProductName);
                }
            }
            else // sequence does not contain N-terminus
            {
                //Digest C-terminus
                if (addCterminalDigestionTruncations)
                {
                    AddCterminalTruncations(lengthOfProteolysis, fullProteinOneBasedEnd, fullProteinOneBasedBegin, minProductBaseSequenceLength, proteolyisisProductName);
                }

                //Digest N-terminus
                if (addNterminalDigestionTruncations)
                {
                    AddNterminalTruncations(lengthOfProteolysis, fullProteinOneBasedBegin, fullProteinOneBasedEnd, minProductBaseSequenceLength, proteolyisisProductName);
                }
            }
        }
        /// <summary>
        /// Returns of list of proteoforms with the specified number of C-terminal amino acid truncations subject to minimum length criteria
        /// </summary>
        private void AddCterminalTruncations(int lengthOfProteolysis, int fullProteinOneBasedEnd, int fullProteinOneBasedBegin, int minProductBaseSequenceLength, string proteolyisisProductName)
        {
            for (int i = 1; i <= lengthOfProteolysis; i++)
            {
                int newEnd = fullProteinOneBasedEnd - i;
                int length = newEnd - fullProteinOneBasedBegin + 1;
                if (length >= minProductBaseSequenceLength)
                {
                    _proteolysisProducts.Add(new ProteolysisProduct(fullProteinOneBasedBegin, newEnd, proteolyisisProductName));
                }
            }
        }
        /// <summary>
        /// Returns of list of proteoforms with the specified number of N-terminal amino acid truncations subject to minimum length criteria
        /// </summary>

        private void AddNterminalTruncations(int lengthOfProteolysis, int fullProteinOneBasedBegin, int fullProteinOneBasedEnd, int minProductBaseSequenceLength, string proteolyisisProductName)
        {
            for (int i = 1; i <= lengthOfProteolysis; i++)
            {
                int newBegin = fullProteinOneBasedBegin + i;
                int length = fullProteinOneBasedEnd - newBegin + 1;
                if (length >= minProductBaseSequenceLength)
                {
                    _proteolysisProducts.Add(new ProteolysisProduct(newBegin, fullProteinOneBasedEnd, proteolyisisProductName));
                }
            }
        }

        /// <summary>
        /// This the main entry point for adding sequences in a top-down truncation search.
        /// The way this is designed is such at all base sequences to be searched end up in the list Protein.ProteolysisProducts
        /// This includes the intact protein. IT DOES NOT INCLUDE ANY DOUBLY (BOTH ENDS) DIGESTED PRODUCTS.
        /// The original proteolysis products (if any) are already in that list. These are annotated in protein.xml files.
        /// The options to keep in mind are present in the following variables
        /// </summary>
        /// <param name="addFullProtein"> This needs to be added to the proteolysisProducts list to be searched </param>
        /// <param name="addForEachOrigninalProteolysisProduct"> the original products are there but those resulting from N- or C-terminal degradation still need to be added</param>
        /// <param name="addNterminalDigestionTruncations"></param>
        /// <param name="addCterminalDigestionTruncations"></param>
        /// <param name="minProductBaseSequenceLength"> the same as the min detectable peptide</param>
        /// <param name="lengthOfProteolysis"> the number of amino acids that can be removed from either end.</param>
        public void AddTruncations(bool addFullProtein = true, bool addForEachOrigninalProteolysisProduct = true, bool addNterminalDigestionTruncations = true, bool addCterminalDigestionTruncations = true, int minProductBaseSequenceLength = 7, int lengthOfProteolysis = 5)
        {
            if (addFullProtein) //this loop adds the intact protoeoform and its proteolysis products to the proteolysis products list
            {
                AddIntactProteoformToTruncationsProducts(minProductBaseSequenceLength);
                if (addNterminalDigestionTruncations)
                {
                    AddTruncationsToExistingProteolysisProducts(1, BaseSequence.Length, true, false, minProductBaseSequenceLength, lengthOfProteolysis, "full-length proteoform N-terminal digestion truncation");
                }
                if (addCterminalDigestionTruncations)
                {
                    AddTruncationsToExistingProteolysisProducts(1, BaseSequence.Length, false, true, minProductBaseSequenceLength, lengthOfProteolysis, "full-length proteoform C-terminal digestion truncation");
                }
            }

            if (addForEachOrigninalProteolysisProduct) // this does not include the original intact proteoform
            {
                List<ProteolysisProduct> existingProducts = ProteolysisProducts.Where(p => !p.Type.Contains("truncation") && !p.Type.Contains("full-length proteoform")).ToList();
                foreach (ProteolysisProduct product in existingProducts)
                {
                    if (product.OneBasedBeginPosition.HasValue && product.OneBasedEndPosition.HasValue)
                    {
                        string proteolyisisProductName = "truncation";

                        if (!String.IsNullOrEmpty(product.Type))
                        {
                            proteolyisisProductName = product.Type + " " + proteolyisisProductName;
                        }
                        //the original proteolysis product is already on the list so we don't need to duplicate
                        if (addNterminalDigestionTruncations)
                        {
                            AddTruncationsToExistingProteolysisProducts(product.OneBasedBeginPosition.Value, product.OneBasedEndPosition.Value, true, false, minProductBaseSequenceLength, lengthOfProteolysis, proteolyisisProductName);
                        }
                        if (addCterminalDigestionTruncations)
                        {
                            AddTruncationsToExistingProteolysisProducts(product.OneBasedBeginPosition.Value, product.OneBasedEndPosition.Value, false, true, minProductBaseSequenceLength, lengthOfProteolysis, proteolyisisProductName);
                        }
                    }
                }
            }
            CleaveOnceBetweenProteolysisProducts();
        }
        /// <summary>
        /// This method adds proteoforms with N- and C-terminal amino acid loss to the list of species included in top-down search
        /// </summary>
        public void AddIntactProteoformToTruncationsProducts(int minProductBaseSequenceLength)
        {
            if (BaseSequence.Length >= minProductBaseSequenceLength)
            {
                _proteolysisProducts.Add(new ProteolysisProduct(1, BaseSequence.Length, "full-length proteoform"));
            }
        }

        /// <summary>
        /// proteins with multiple proteolysis products are not always full cleaved. we observed proteolysis products w/ missed cleavages.
        /// This method allows for one missed cleavage between proteolysis products.
        /// </summary>

        public void CleaveOnceBetweenProteolysisProducts(int minimumProductLength = 7)
        {
            List<int> cleavagePostions = new();
            List<ProteolysisProduct> localProducts = _proteolysisProducts.Where(p => !p.Type.Contains("truncation") && !p.Type.Contains("full-length proteoform")).ToList();
            List<int> proteolysisProductEndPositions = localProducts.Where(p => p.OneBasedEndPosition.HasValue).Select(p => p.OneBasedEndPosition.Value).ToList();
            if (proteolysisProductEndPositions.Count > 0)
            {
                foreach (int proteolysisProductEndPosition in proteolysisProductEndPositions)
                {
                    if (localProducts.Any(p => p.OneBasedBeginPosition == (proteolysisProductEndPosition + 1)))
                    {
                        cleavagePostions.Add(proteolysisProductEndPosition);
                    }
                }
            }

            foreach (int position in cleavagePostions)
            {
                if (position - 1 >= minimumProductLength)
                {
                    string leftType = $"N-terminal Portion of Singly Cleaved Protein(1-{position})";
                    ProteolysisProduct leftProduct = new(1, position, leftType);

                    //here we're making sure a product with these begin/end positions isn't already present
                    if (!_proteolysisProducts.Any(p => p.OneBasedBeginPosition == leftProduct.OneBasedBeginPosition && p.OneBasedEndPosition == leftProduct.OneBasedEndPosition))
                    {
                        _proteolysisProducts.Add(leftProduct);
                    }
                }

                if (BaseSequence.Length - position - 1 >= minimumProductLength)
                {
                    string rightType = $"C-terminal Portion of Singly Cleaved Protein({position + 1}-{BaseSequence.Length})";
                    ProteolysisProduct rightProduct = new(position + 1, BaseSequence.Length, rightType);

                    //here we're making sure a product with these begin/end positions isn't already present
                    if (!_proteolysisProducts.Any(p => p.OneBasedBeginPosition == rightProduct.OneBasedBeginPosition && p.OneBasedEndPosition == rightProduct.OneBasedEndPosition))
                    {
                        _proteolysisProducts.Add(rightProduct);
                    }
                }
            }
        }

        private static string GetName(IEnumerable<SequenceVariation> appliedVariations, string name)
        {
            bool emptyVars = appliedVariations == null || appliedVariations.Count() == 0;
            if (name == null && emptyVars)
            {
                return null;
            }
            else
            {
                string variantTag = emptyVars ? "" : $" variant:{VariantApplication.CombineDescriptions(appliedVariations)}";
                return name + variantTag;
            }
        }

        /// <summary>
        /// This function takes in a decoy protein and a list of forbidden sequences that the decoy
        /// protein should not contain. Optionally, a list of the peptides within the base sequence
        /// of the decoy protein that need to be scrambled can be passed as well. It will scramble the required sequences,
        /// leaving cleavage sites intact. 
        /// </summary>
        /// <param name="originalDecoyProtein"> A Decoy protein to be cloned </param>
        /// <param name="digestionParams"> Digestion parameters </param>
        /// <param name="forbiddenSequences"> A HashSet of forbidden sequences that the decoy protein should not contain. Typically, a set of target base sequences </param>
        /// <param name="sequencesToScramble"> Optional IEnumberable of sequences within the decoy protein that need to be replaced.
        ///                                     If this is passed, only sequences within the IEnumerable will be replaced!!! </param>
        /// <returns> A cloned copy of the decoy protein with a scrambled sequence </returns>
        public static Protein ScrambleDecoyProteinSequence(
            Protein originalDecoyProtein,
            DigestionParams digestionParams,
            HashSet<string> forbiddenSequences,
            IEnumerable<string> sequencesToScramble = null)
        {
            // If no sequencesToScramble are passed in, we check to see if any 
            // peptides in the decoy are forbidden sequences
            sequencesToScramble = sequencesToScramble ?? originalDecoyProtein
                .Digest(digestionParams, new List<Modification>(), new List<Modification>())
                .Select(pep => pep.FullSequence)
                .Where(forbiddenSequences.Contains);
            if(sequencesToScramble.Count() == 0)
            {
                return originalDecoyProtein;
            }

            string scrambledProteinSequence = originalDecoyProtein.BaseSequence;
            // Clone the original protein's modifications
            var scrambledModificationDictionary = originalDecoyProtein.OriginalNonVariantModifications.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            // Start small and then go big. If we scramble a zero-missed cleavage peptide, but the missed cleavage peptide contains the previously scrambled peptide
            // Then we can avoid unnecessary operations as the scrambledProteinSequence will no longer contain the longer sequence of the missed cleavage peptide
            foreach(string peptideSequence in sequencesToScramble.OrderBy(seq => seq.Length))
            {
                if(scrambledProteinSequence.Contains(peptideSequence))
                {
                    string scrambledPeptideSequence = ScrambleSequence(peptideSequence, digestionParams.DigestionAgent.DigestionMotifs, 
                        out var swappedArray);
                    int scrambleAttempts = 1;

                    // Try five times to scramble the peptide sequence without creating a forbidden sequence
                    while(forbiddenSequences.Contains(scrambledPeptideSequence) & scrambleAttempts <= 5)
                    {
                        scrambledPeptideSequence = ScrambleSequence(peptideSequence, digestionParams.DigestionAgent.DigestionMotifs,
                            out swappedArray);
                        scrambleAttempts++;
                    }

                    scrambledProteinSequence = scrambledProteinSequence.Replace(peptideSequence, scrambledPeptideSequence);

                    if (!scrambledModificationDictionary.Any()) continue;

                    // rearrange the modifications 
                    foreach (int index in scrambledProteinSequence.IndexOfAll(scrambledPeptideSequence))
                    {
                        // Get mods that were affected by the scramble
                        var relevantMods = scrambledModificationDictionary.Where(kvp => 
                            kvp.Key >= index + 1 && kvp.Key < index + peptideSequence.Length + 1).ToList();

                        // Modify the dictionary to reflect the new positions of the modifications
                        foreach (var kvp in relevantMods)
                        {
                            int newKey = swappedArray[kvp.Key - 1 - index] + 1 + index;
                            // To prevent collisions, we have to check if mods already exist at the new idx.
                            if(scrambledModificationDictionary.TryGetValue(newKey, out var modsToSwap))
                            {
                                // If there are mods at the new idx, we swap the mods
                                scrambledModificationDictionary[newKey] = kvp.Value;
                                scrambledModificationDictionary[kvp.Key] = modsToSwap;
                            }
                            else
                            {
                                scrambledModificationDictionary.Add(newKey, kvp.Value);
                                scrambledModificationDictionary.Remove(kvp.Key);
                            }
                        }
                    }
                }
            }

            Protein newProtein = new Protein(originalDecoyProtein, scrambledProteinSequence);

            // Update the modifications using the scrambledModificationDictionary
            newProtein.OriginalNonVariantModifications = scrambledModificationDictionary;
            newProtein.OneBasedPossibleLocalizedModifications = newProtein.SelectValidOneBaseMods(scrambledModificationDictionary);
            
            return newProtein;
        }

        private static Random rng = new Random(42);

        /// <summary>
        /// Scrambles a peptide sequence, preserving the position of any cleavage sites.
        /// </summary>
        /// <param name="swappedPositionArray">An array that maps the previous position (index) to the new position (value)</param>
        public static string ScrambleSequence(string sequence, List<DigestionMotif> motifs, out int[] swappedPositionArray)
        {
            // First, find the location of every cleavage motif. These sites shouldn't be scrambled.
            HashSet<int> zeroBasedCleavageSitesLocations = new();
            foreach (var motif in motifs)
            {
                for (int i = 0; i < sequence.Length; i++)
                {
                    (bool fits, bool prevents) = motif.Fits(sequence, i);
                    if (fits && !prevents)
                    {
                        zeroBasedCleavageSitesLocations.Add(i);
                    }
                }
            }

            // Next, scramble the sequence using the Fisher-Yates shuffle algorithm.
            char[] sequenceArray = sequence.ToCharArray();
            // We're going to keep track of the positions of the characters in the original sequence,
            // This will enable us to adjust the location of modifications that are present in the original sequence
            // to the new scrambled sequence.
            int[] tempPositionArray = Enumerable.Range(0, sequenceArray.Length).ToArray();
            int n = sequenceArray.Length;
            while(n > 1)
            {
                n--;
                if(zeroBasedCleavageSitesLocations.Contains(n))
                {
                    // Leave the cleavage site in place
                    continue;
                }
                int k = rng.Next(n + 1);
                // don't swap the position of a cleavage site
                while(zeroBasedCleavageSitesLocations.Contains(k))
                {
                    k = rng.Next(n + 1);
                }

                // rearrange the sequence array
                char tempResidue = sequenceArray[k];
                sequenceArray[k] = sequenceArray[n];
                sequenceArray[n] = tempResidue;

                // update the position array to represent the swaps
                int tempPosition = tempPositionArray[k];
                tempPositionArray[k] = tempPositionArray[n];
                tempPositionArray[n] = tempPosition;
            }

            // This maps the previous position (index) to the new position (value)
            swappedPositionArray = new int[tempPositionArray.Length];
            for (int i = 0; i < tempPositionArray.Length; i++)
            {
                swappedPositionArray[tempPositionArray[i]] = i;
            }

            return new string(sequenceArray);
        }

        public int CompareTo(Protein other)
        {
            //permits sorting of proteins
            return this.Accession.CompareTo(other.Accession);
        }

        //not sure if we require any additional fields for equality
        public override bool Equals(object obj)
        {
            Protein otherProtein = (Protein)obj;
            return otherProtein != null && otherProtein.Accession.Equals(Accession) && otherProtein.BaseSequence.Equals(BaseSequence);
        }

        /// <summary>
        /// The protein object uses the default hash code method for speed,
        /// but note that two protein objects with the same information will give two different hash codes.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return this.BaseSequence.GetHashCode();
        }

        public override string ToString()
        {
            return this.Accession.ToString();
        }
    }
}