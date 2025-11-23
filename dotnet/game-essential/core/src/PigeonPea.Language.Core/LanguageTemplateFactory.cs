using FantasyNameGenerator.Phonology;
using FantasyNameGenerator.Phonotactics;
using FantasyNameGenerator.Morphology;

namespace PigeonPea.Language.Core;

/// <summary>
/// Factory for creating language templates for FMG name generator
/// </summary>
public class LanguageTemplateFactory
{
    /// <summary>
    /// Creates phonology template for the specified language
    /// </summary>
    public CulturePhonology CreatePhonology(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "germanic" => CreateGermanicPhonology(),
            "elvish" => CreateElvishPhonology(),
            "dwarvish" => CreateDwarvishPhonology(),
            "orcish" => CreateOrcishPhonology(),
            "japanese" => CreateJapanesePhonology(),
            "chinese" => CreateChinesePhonology(),
            "korean" => CreateKoreanPhonology(),
            "slavic" => CreateSlavicPhonology(),
            "romance" => CreateRomancePhonology(),
            "nordic" => CreateNordicPhonology(),
            "celtic" => CreateCelticPhonology(),
            "arabic" => CreateArabicPhonology(),
            _ => CreateGermanicPhonology()
        };
    }

    /// <summary>
    /// Creates phonotactic rules for the specified language
    /// </summary>
    public PhonotacticRules CreatePhonotactics(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "germanic" => CreateGermanicPhonotactics(),
            "elvish" => CreateElvishPhonotactics(),
            "dwarvish" => CreateDwarvishPhonotactics(),
            "orcish" => CreateOrcishPhonotactics(),
            "japanese" => CreateJapanesePhonotactics(),
            "chinese" => CreateChinesePhonotactics(),
            "korean" => CreateKoreanPhonotactics(),
            "slavic" => CreateSlavicPhonotactics(),
            "romance" => CreateRomancePhonotactics(),
            "nordic" => CreateNordicPhonotactics(),
            "celtic" => CreateCelticPhonotactics(),
            "arabic" => CreateArabicPhonotactics(),
            _ => CreateGermanicPhonotactics()
        };
    }

    /// <summary>
    /// Creates morphology rules for the specified language
    /// </summary>
    public MorphologyRules CreateMorphology(string templateName)
    {
        return templateName.ToLowerInvariant() switch
        {
            "germanic" => CreateGermanicMorphology(),
            "elvish" => CreateElvishMorphology(),
            "dwarvish" => CreateDwarvishMorphology(),
            "orcish" => CreateOrcishMorphology(),
            "japanese" => CreateJapaneseMorphology(),
            "chinese" => CreateChineseMorphology(),
            "korean" => CreateKoreanMorphology(),
            "slavic" => CreateSlavicMorphology(),
            "romance" => CreateRomanceMorphology(),
            "nordic" => CreateNordicMorphology(),
            "celtic" => CreateCelticMorphology(),
            "arabic" => CreateArabicMorphology(),
            _ => CreateGermanicMorphology()
        };
    }

    #region Phonology Templates

    private CulturePhonology CreateGermanicPhonology()
    {
        return new CulturePhonology
        {
            Name = "Germanic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdgfsʃʒmnŋlrwjh",
                Vowels = "aeiouyæøœɑɔɛɪʏʊ",
                Liquids = "lrw",
                Nasals = "mnŋ",
                Fricatives = "fsʃʒh",
                Stops = "ptkbdg"
            }
        };
    }

    private CulturePhonology CreateElvishPhonology()
    {
        return new CulturePhonology
        {
            Name = "Elvish",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsʃθmnŋlrwvj",
                Vowels = "aeiouyɛɪɔʊ",
                Liquids = "lrw",
                Nasals = "mnŋ",
                Fricatives = "fsʃθv",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateDwarvishPhonology()
    {
        return new CulturePhonology
        {
            Name = "Dwarvish",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡkʰɡʰfsʃxzmnŋlr",
                Vowels = "aeiouɔɛʊ",
                Liquids = "lr",
                Nasals = "mnŋ",
                Fricatives = "fsʃxz",
                Stops = "ptkbdɡkʰɡʰ"
            }
        };
    }

    private CulturePhonology CreateOrcishPhonology()
    {
        return new CulturePhonology
        {
            Name = "Orcish",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡɢʔfsʃχmnŋlrɡʁ",
                Vowels = "auɔɑ",
                Liquids = "lr",
                Nasals = "mnŋ",
                Fricatives = "fsʃχɡʁ",
                Stops = "ptkbdɡɢʔ"
            }
        };
    }

    private CulturePhonology CreateJapanesePhonology()
    {
        return new CulturePhonology
        {
            Name = "Japanese",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsʃmnŋlrw",
                Vowels = "aeiou",
                Liquids = "r",
                Nasals = "mnŋ",
                Fricatives = "fsʃ",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateChinesePhonology()
    {
        return new CulturePhonology
        {
            Name = "Chinese",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡtsʨʨʃmnŋ",
                Vowels = "aeiouy",
                Liquids = "",
                Nasals = "mnŋ",
                Fricatives = "fsʃʨʨ",
                Stops = "ptkbdɡts"
            }
        };
    }

    private CulturePhonology CreateKoreanPhonology()
    {
        return new CulturePhonology
        {
            Name = "Korean",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡkʰsʃmnŋlr",
                Vowels = "aeiou",
                Liquids = "lr",
                Nasals = "mnŋ",
                Fricatives = "sʃ",
                Stops = "ptkbdɡkʰ"
            }
        };
    }

    private CulturePhonology CreateSlavicPhonology()
    {
        return new CulturePhonology
        {
            Name = "Slavic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsʃʒʦʧʨmnŋlrjv",
                Vowels = "aeiouyɨ",
                Liquids = "lrj",
                Nasals = "mnŋ",
                Fricatives = "fsʃʒʦʧʨv",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateRomancePhonology()
    {
        return new CulturePhonology
        {
            Name = "Romance",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsʃmnŋlrjv",
                Vowels = "aeiouyɛɔ",
                Liquids = "lrj",
                Nasals = "mnŋ",
                Fricatives = "fsʃv",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateNordicPhonology()
    {
        return new CulturePhonology
        {
            Name = "Nordic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsθðmnŋlr",
                Vowels = "aeiouyæøœɔɛ",
                Liquids = "lr",
                Nasals = "mnŋ",
                Fricatives = "fsθð",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateCelticPhonology()
    {
        return new CulturePhonology
        {
            Name = "Celtic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptkbdɡfsʃmnŋlrw",
                Vowels = "aeiouyɛɔɪʊ",
                Liquids = "lrw",
                Nasals = "mnŋ",
                Fricatives = "fsʃ",
                Stops = "ptkbdɡ"
            }
        };
    }

    private CulturePhonology CreateArabicPhonology()
    {
        return new CulturePhonology
        {
            Name = "Arabic",
            Inventory = new PhonemeInventory
            {
                Consonants = "ptbdɡkqfsʃχʁmnŋlr",
                Vowels = "aiu",
                Liquids = "lr",
                Nasals = "mnŋ",
                Fricatives = "fsʃχʁ",
                Stops = "ptbdɡkq"
            }
        };
    }

    #endregion

    #region Phonotactics Templates

    private PhonotacticRules CreateGermanicPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVC", "CCVC", "CVCC" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3
        };
    }

    private PhonotacticRules CreateElvishPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "VCV", "CVCV", "V" },
            MaxConsonantCluster = 1,
            MinSyllables = 2,
            MaxSyllables = 4
        };
    }

    private PhonotacticRules CreateDwarvishPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CVC", "CCVC", "CVCC", "CCVCC" },
            MaxConsonantCluster = 3,
            MinSyllables = 1,
            MaxSyllables = 2
        };
    }

    private PhonotacticRules CreateOrcishPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CVC", "CC", "VC", "CV" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 2
        };
    }

    private PhonotacticRules CreateJapanesePhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVV" },
            MaxConsonantCluster = 1,
            MinSyllables = 2,
            MaxSyllables = 4
        };
    }

    private PhonotacticRules CreateChinesePhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "C", "V" },
            MaxConsonantCluster = 1,
            MinSyllables = 1,
            MaxSyllables = 2
        };
    }

    private PhonotacticRules CreateKoreanPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVC", "V" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3
        };
    }

    private PhonotacticRules CreateSlavicPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CVC", "CV", "CCVC", "CVCC" },
            MaxConsonantCluster = 3,
            MinSyllables = 1,
            MaxSyllables = 3
        };
    }

    private PhonotacticRules CreateRomancePhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVC", "VCV", "V" },
            MaxConsonantCluster = 2,
            MinSyllables = 2,
            MaxSyllables = 4
        };
    }

    private PhonotacticRules CreateNordicPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVC", "CCVC", "CVCC" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3
        };
    }

    private PhonotacticRules CreateCelticPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CV", "CVC", "VCV", "V" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 4
        };
    }

    private PhonotacticRules CreateArabicPhonotactics()
    {
        return new PhonotacticRules
        {
            Structures = new[] { "CVC", "CV", "VC" },
            MaxConsonantCluster = 2,
            MinSyllables = 1,
            MaxSyllables = 3
        };
    }

    #endregion

    #region Morphology Templates

    private MorphologyRules CreateGermanicMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "be", Meaning = "around", Frequency = 0.1 },
                new Morpheme { Form = "for", Meaning = "before", Frequency = 0.1 },
                new Morpheme { Form = "un", Meaning = "not", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "heim", Meaning = "home", Frequency = 0.1 },
                new Morpheme { Form = "burg", Meaning = "fort", Frequency = 0.1 },
                new Morpheme { Form = "ton", Meaning = "town", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateElvishMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "el", Meaning = "star", Frequency = 0.1 },
                new Morpheme { Form = "gal", Meaning = "light", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "dor", Meaning = "land", Frequency = 0.1 },
                new Morpheme { Form = "ion", Meaning = "son", Frequency = 0.1 },
                new Morpheme { Form = "eth", Meaning = "first", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateDwarvishMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "dur", Meaning = "deep", Frequency = 0.1 },
                new Morpheme { Form = "az", Meaning = "iron", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "kar", Meaning = "hall", Frequency = 0.1 },
                new Morpheme { Form = "dum", Meaning = "delving", Frequency = 0.1 },
                new Morpheme { Form = "rod", Meaning = "mountain", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateOrcishMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "gh", Meaning = "blood", Frequency = 0.1 },
                new Morpheme { Form = "kr", Meaning = "kill", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "ak", Meaning = "place", Frequency = 0.1 },
                new Morpheme { Form = "ug", Meaning = "dark", Frequency = 0.1 },
                new Morpheme { Form = "oth", Meaning = "fortress", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateJapaneseMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "o", Meaning = "big", Frequency = 0.1 },
                new Morpheme { Form = "ko", Meaning = "small", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "machi", Meaning = "town", Frequency = 0.1 },
                new Morpheme { Form = "jima", Meaning = "island", Frequency = 0.1 },
                new Morpheme { Form = "saki", Meaning = "cape", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateChineseMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "da", Meaning = "big", Frequency = 0.1 },
                new Morpheme { Form = "xiao", Meaning = "small", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "shan", Meaning = "mountain", Frequency = 0.1 },
                new Morpheme { Form = "he", Meaning = "river", Frequency = 0.1 },
                new Morpheme { Form = "lin", Meaning = "forest", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateKoreanMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "dae", Meaning = "big", Frequency = 0.1 },
                new Morpheme { Form = "so", Meaning = "small", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "san", Meaning = "mountain", Frequency = 0.1 },
                new Morpheme { Form = "gang", Meaning = "river", Frequency = 0.1 },
                new Morpheme { Form = "ri", Meaning = "village", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateSlavicMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "nov", Meaning = "new", Frequency = 0.1 },
                new Morpheme { Form = "star", Meaning = "old", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "grad", Meaning = "city", Frequency = 0.1 },
                new Morpheme { Form = "sk", Meaning = "place", Frequency = 0.1 },
                new Morpheme { Form = "ov", Meaning = "of", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateRomanceMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "bon", Meaning = "good", Frequency = 0.1 },
                new Morpheme { Form = "mal", Meaning = "bad", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "ville", Meaning = "town", Frequency = 0.1 },
                new Morpheme { Form = "mont", Meaning = "mountain", Frequency = 0.1 },
                new Morpheme { Form = "port", Meaning = "harbor", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateNordicMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "nord", Meaning = "north", Frequency = 0.1 },
                new Morpheme { Form = "sør", Meaning = "south", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "fjord", Meaning = "fjord", Frequency = 0.1 },
                new Morpheme { Form = "vik", Meaning = "bay", Frequency = 0.1 },
                new Morpheme { Form = "holm", Meaning = "island", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateCelticMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "aber", Meaning = "river", Frequency = 0.1 },
                new Morpheme { Form = "dun", Meaning = "fort", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "more", Meaning = "great", Frequency = 0.1 },
                new Morpheme { Form = "wich", Meaning = "place", Frequency = 0.1 },
                new Morpheme { Form = "don", Meaning = "fort", Frequency = 0.1 }
            }
        };
    }

    private MorphologyRules CreateArabicMorphology()
    {
        return new MorphologyRules
        {
            Prefixes = new[]
            {
                new Morpheme { Form = "al", Meaning = "the", Frequency = 0.2 },
                new Morpheme { Form = "bi", Meaning = "with", Frequency = 0.1 }
            },
            Suffixes = new[]
            {
                new Morpheme { Form = "abad", Meaning = "inhabited", Frequency = 0.1 },
                new Morpheme { Form = "qasr", Meaning = "castle", Frequency = 0.1 },
                new Morpheme { Form = "wadi", Meaning = "valley", Frequency = 0.1 }
            }
        };
    }

    #endregion
}
