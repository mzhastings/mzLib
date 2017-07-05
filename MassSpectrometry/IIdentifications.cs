﻿using MzLibUtil;

namespace MassSpectrometry
{
    public interface IIdentifications
    {
        #region Public Properties

        int Count { get; }

        Tolerance ParentTolerance { get; }

        Tolerance FragmentTolerance { get; }

        #endregion Public Properties

        #region Public Methods

        string Ms2SpectrumID(int matchIndex);

        int ChargeState(int matchIndex, int siiIndex);

        float[] MatchedIons(int matchIndex, int siiIndex, int i);

        int MatchedIonCounts(int matchIndex, int siiIndex, int i);

        string ProteinAccession(int matchIndex, int siiIndex);

        string ProteinFullName(int matchIndex, int siiIndex);

        string StartResidueInProtein(int matchIndex, int siiIndex);

        string EndResidueInProtein(int matchIndex, int siiIndex);

        bool IsDecoy(int matchIndex, int siiIndex);

        double QValue(int matchIndex, int siiIndex);

        double CalculatedMassToCharge(int matchIndex, int siiIndex);

        double ExperimentalMassToCharge(int matchIndex, int siiIndex);

        string PeptideSequenceWithoutModifications(int matchIndex, int siiIndex);

        int NumModifications(int matchIndex, int siiIndex);

        int ModificationLocation(int matchIndex, int siiIndex, int i);

        string ModificationDictionary(int matchIndex, int siiIndex, int i);

        string ModificationAcession(int matchIndex, int siiIndex, int i);

        int NumPSMsFromScan(int matchIndex);

        double ModificationMass(int matchIndex, int siiIndex, int i);

        #endregion Public Methods
    }
}