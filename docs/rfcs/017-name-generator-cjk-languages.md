---
canonical: true
created: '2025-11-15'
doc_id: RFC-00017
doc_type: rfc
related:
  - SPEC-00014
  - RFC-00016
status: draft
summary: Add East Asian (Chinese, Japanese, Korean) language templates with proper
  phonology and writing systems
supersedes: []
tags:
  - name-generation
  - cjk
  - japanese
  - chinese
  - korean
  - languages
  - i18n
title: Name Generator CJK Language Support
---

# RFC 017: Name Generator CJK Language Support

## Status

- **State:** Draft
- **Priority:** ⭐⭐⭐⭐ High
- **Estimated Effort:** 3-4 days
- **Dependencies:** RFC 016 (JSON Configuration)
- **Target:** FantasyNameGenerator v2.0

## Problem Statement

The current FantasyNameGenerator **lacks East Asian language support**. The 6 existing templates are all Western or Western-fantasy inspired:

**Current Templates:**

- Germanic, Romance, Slavic (European)
- Elvish, Dwarvish, Orcish (Western fantasy)

**Missing:**

- Japanese (日本語)
- Chinese (中文) - Mandarin
- Korean (한국어)

This limits the generator's usefulness for:

- East Asian-inspired fantasy settings
- Diverse cultural representation
- Global game development
- Historical East Asian settings

## Proposed Solution

Add **3 CJK language templates** with linguistically-accurate phonology:

1. **Japanese** - Mora-based, simple syllables, borrowed Chinese words
2. **Chinese (Mandarin)** - Tonal, limited syllables, 4 tones
3. **Korean** - Complex syllable blocks, agglutinative

Each template will include:

- Authentic phoneme inventory
- Real phonotactic constraints
- Romanization (Hepburn, Pinyin, Revised Romanization)
- Writing system notes (though output is romanized)
- Cultural naming patterns

## Japanese Language Template

### Phonological Features

**Phoneme Inventory:**

- Consonants: /p, t, k, b, d, g, m, n, s, z, h, r, w, j/
- Vowels: /a, i, u, e, o/
- No consonant clusters
- No syllable-final consonants (except /n/)

**Mora-Based:**

- CV (ka, ki, ku, ke, ko)
- V (a, i, u, e, o)
- N (syllabic n)
- Geminate consonants (kka, tta)

**Forbidden Sequences:**

- No /ti, di/ (becomes /chi, ji/)
- No /tu, du/ (becomes /tsu, zu/)
- No /si/ (becomes /shi/)
- No /hu/ (becomes /fu/)
- No /wo/ (only particle)

### Japanese Template JSON

```json
{
  "name": "japanese",
  "version": "1.0.0",
  "description": "Japanese language (日本語) - Mora-based, simple syllable structure",
  "author": "FantasyMapGenerator",
  "tags": ["real-world", "asian", "japanese", "cjk"],

  "phonology": {
    "consonants": "ptkbdgmnszhrwj",
    "vowels": "aiueo",
    "liquids": "r",
    "nasals": "mn",
    "fricatives": "szh",
    "stops": "ptkbdg",
    "sibilants": "sz",
    "finals": "n",
    "allophones": [
      { "phoneme": "t", "allophone": "ʧ", "context": "i" },
      { "phoneme": "d", "allophone": "ʤ", "context": "i" },
      { "phoneme": "s", "allophone": "ʃ", "context": "i" },
      { "phoneme": "h", "allophone": "ɸ", "context": "u" }
    ],
    "orthography": {
      "consonants": {
        "ʧ": "ch",
        "ʤ": "j",
        "ʃ": "sh",
        "ɸ": "f"
      }
    }
  },

  "phonotactics": {
    "structures": ["CV", "V", "CVN"],
    "forbiddenSequences": ["ti", "di", "tu", "du", "si", "hu", "wo", "yi", "wu"],
    "allowedOnsets": [
      "k",
      "s",
      "t",
      "n",
      "h",
      "m",
      "r",
      "w",
      "g",
      "z",
      "d",
      "b",
      "p",
      "ky",
      "sh",
      "ch",
      "ny",
      "hy",
      "my",
      "ry",
      "gy",
      "j",
      "by",
      "py"
    ],
    "allowedCodas": ["n"],
    "maxConsonantCluster": 1,
    "maxVowelCluster": 2,
    "minSyllables": 2,
    "maxSyllables": 4,
    "enforceSonoritySequencing": false
  },

  "morphology": {
    "suffixes": [
      { "form": "yama", "meaning": "mountain", "frequency": 0.2 },
      { "form": "kawa", "meaning": "river", "frequency": 0.2 },
      { "form": "shima", "meaning": "island", "frequency": 0.1 },
      { "form": "mura", "meaning": "village", "frequency": 0.15 },
      { "form": "dera", "meaning": "temple", "frequency": 0.1 },
      { "form": "jō", "meaning": "castle", "frequency": 0.1 },
      { "form": "no", "meaning": "of/field", "frequency": 0.15 }
    ],
    "compounding": {
      "joiner": "",
      "headFirst": false,
      "probability": 0.6
    }
  },

  "grammar": {
    "wordOrder": "SOV",
    "joiner": "",
    "genitive": "no"
  },

  "exponent": 1.2
}
```

### Japanese Naming Patterns

**Place Names:**

- Mountains: ~yama, ~san (富士山 Fujisan → Fujiyama)
- Rivers: ~kawa, ~gawa (利根川 → Tonegawa)
- Cities: ~shi, ~machi (京都 → Kyoto)
- Villages: ~mura (白川村 → Shirakawamura)
- Castles: ~jō (大阪城 → Ōsakajō)

**Compound Structure:**

- Descriptive + Place Type
- Examples: Shirakawa-mura (White River Village), Fujiyama (Fuji Mountain)

## Chinese (Mandarin) Language Template

### Phonological Features

**Phoneme Inventory:**

- Initial consonants: /p, t, k, m, n, f, s, x, ʂ, tʂ, tɕ, etc./
- Finals: /a, o, e, i, u, ü/ + /n, ŋ/
- Limited syllable inventory (~400 base syllables)
- 4 tones (but we'll ignore tones in generation)

**Syllable Structure:**

- (C)(G)V(C) where G = /j, w/, C = /n, ŋ/
- No consonant clusters
- Only /n, ŋ/ syllable-final

### Chinese Template JSON

```json
{
  "name": "chinese",
  "version": "1.0.0",
  "description": "Mandarin Chinese (中文) - Tonal, limited syllable inventory",
  "author": "FantasyMapGenerator",
  "tags": ["real-world", "asian", "chinese", "cjk", "mandarin"],

  "phonology": {
    "consonants": "ptkmnlsfʃʂʐhwjtɕx",
    "vowels": "aeiouəɤy",
    "liquids": "lr",
    "nasals": "mnŋ",
    "fricatives": "fsʃʂʐx",
    "stops": "ptk",
    "sibilants": "sʃʂʐ",
    "finals": "nŋ",
    "orthography": {
      "consonants": {
        "ʃ": "sh",
        "ʂ": "sh",
        "ʐ": "r",
        "tɕ": "j",
        "ɕ": "x"
      },
      "vowels": {
        "ə": "e",
        "ɤ": "e",
        "y": "ü"
      }
    }
  },

  "phonotactics": {
    "structures": ["CV", "CVN", "V", "VN"],
    "forbiddenSequences": ["tp", "kt", "pk", "mn", "nm"],
    "allowedOnsets": [
      "p",
      "t",
      "k",
      "m",
      "n",
      "l",
      "f",
      "s",
      "sh",
      "h",
      "w",
      "y",
      "j",
      "q",
      "x",
      "zh",
      "ch",
      "r"
    ],
    "allowedCodas": ["n", "ng"],
    "maxConsonantCluster": 1,
    "maxVowelCluster": 3,
    "minSyllables": 1,
    "maxSyllables": 3,
    "enforceSonoritySequencing": false
  },

  "morphology": {
    "suffixes": [
      { "form": "shān", "meaning": "mountain", "frequency": 0.2 },
      { "form": "hé", "meaning": "river", "frequency": 0.2 },
      { "form": "chéng", "meaning": "city", "frequency": 0.15 },
      { "form": "zhōu", "meaning": "prefecture", "frequency": 0.1 },
      { "form": "dǎo", "meaning": "island", "frequency": 0.1 },
      { "form": "hú", "meaning": "lake", "frequency": 0.1 }
    ],
    "compounding": {
      "joiner": "",
      "headFirst": true,
      "probability": 0.7
    }
  },

  "grammar": {
    "wordOrder": "SVO",
    "joiner": "",
    "genitive": "de"
  },

  "exponent": 1.5
}
```

### Chinese Naming Patterns

**Place Names:**

- Mountains: ~山 shān (黄山 Huangshan → Yellow Mountain)
- Rivers: ~河 hé, ~江 jiāng (长江 Changjiang → Long River)
- Cities: ~城 chéng (北京 Beijing → North Capital)
- Provinces: ~省 shěng (山东 Shandong → East of Mountains)

**Structure:**

- Usually 2-3 syllables
- Descriptive + Type
- Compound adjectives common

## Korean Language Template

### Phonological Features

**Phoneme Inventory:**

- Consonants: /p, t, k, m, n, l, s, h, tɕ/ + aspirated/tense variants
- Vowels: /a, e, i, o, u, ɯ, ʌ/
- Complex syllable blocks (CVC)
- 3-way contrast: plain, aspirated, tense

**Syllable Structure:**

- (C)(G)V(C) where G = glide
- Syllable-final: /p, t, k, m, n, ŋ, l/

### Korean Template JSON

```json
{
  "name": "korean",
  "version": "1.0.0",
  "description": "Korean language (한국어) - Complex syllable blocks, agglutinative",
  "author": "FantasyMapGenerator",
  "tags": ["real-world", "asian", "korean", "cjk"],

  "phonology": {
    "consonants": "ptkmnlshjtɕ",
    "vowels": "aeiouəʌɯ",
    "liquids": "lr",
    "nasals": "mnŋ",
    "fricatives": "sh",
    "stops": "ptk",
    "sibilants": "s",
    "finals": "ptkmnŋl",
    "orthography": {
      "consonants": {
        "tɕ": "j",
        "ŋ": "ng"
      },
      "vowels": {
        "ə": "eo",
        "ʌ": "eo",
        "ɯ": "eu"
      }
    }
  },

  "phonotactics": {
    "structures": ["CV", "CVC", "V", "VC"],
    "forbiddenSequences": ["ŋk", "ŋp", "ŋt"],
    "allowedOnsets": ["k", "n", "t", "l", "m", "p", "s", "ng", "j", "ch", "h"],
    "allowedCodas": ["k", "n", "t", "l", "m", "p", "ng"],
    "maxConsonantCluster": 1,
    "maxVowelCluster": 2,
    "minSyllables": 2,
    "maxSyllables": 4,
    "enforceSonoritySequencing": false
  },

  "morphology": {
    "suffixes": [
      { "form": "san", "meaning": "mountain", "frequency": 0.2 },
      { "form": "gang", "meaning": "river", "frequency": 0.2 },
      { "form": "do", "meaning": "province", "frequency": 0.15 },
      { "form": "si", "meaning": "city", "frequency": 0.15 },
      { "form": "gun", "meaning": "county", "frequency": 0.1 },
      { "form": "ri", "meaning": "village", "frequency": 0.1 }
    ],
    "compounding": {
      "joiner": "",
      "headFirst": true,
      "probability": 0.5
    }
  },

  "grammar": {
    "wordOrder": "SOV",
    "joiner": "",
    "genitive": "ui"
  },

  "exponent": 1.3
}
```

### Korean Naming Patterns

**Place Names:**

- Mountains: ~산 san (백두산 Baekdusan → White Head Mountain)
- Rivers: ~강 gang (한강 Hangang → Han River)
- Cities: ~시 si (서울시 Seoulsi → Seoul City)
- Provinces: ~도 do (경기도 Gyeonggido → Gyeonggi Province)

**Structure:**

- 2-4 syllables common
- Native Korean + Sino-Korean mixed
- Agglutinative suffixes

## Implementation Plan

### Day 1: JSON Template Creation

- [ ] Create `japanese.json` with phonology, phonotactics, morphology
- [ ] Create `chinese.json` with Mandarin features
- [ ] Create `korean.json` with Hangul-inspired romanization
- [ ] Validate against JSON schema

### Day 2: Orthography & Special Rules

- [ ] Implement Japanese allophonic rules (ti→chi, si→shi, etc.)
- [ ] Implement Chinese tone-neutral romanization (Pinyin-style)
- [ ] Implement Korean romanization (Revised Romanization)
- [ ] Test phoneme generation

### Day 3: Testing & Validation

- [ ] Generate 1000+ names per language
- [ ] Verify phonotactic constraints
- [ ] Check for pronounceability
- [ ] Validate cultural appropriateness
- [ ] Compare with real place names

### Day 4: Integration & Documentation

- [ ] Add to `PhonologyTemplates`
- [ ] Update `CultureType` enum with Asian cultures
- [ ] Write usage documentation
- [ ] Create example outputs
- [ ] Add to README

## Example Outputs

### Japanese Names

**Burgs:**

- Sakuramura (Cherry Blossom Village)
- Takayama (High Mountain)
- Miyakawa (Sacred River)
- Shirokawajō (White River Castle)

**States:**

- Hinokunino (Land of the Sun)
- Yamashironokuni (Mountain Castle Land)
- Mizuho (Abundant Rice Ears)

### Chinese Names

**Burgs:**

- Lóngchéng (Dragon City)
- Báihé (White River)
- Jīnshān (Gold Mountain)
- Qīnghú (Clear Lake)

**States:**

- Zhōngguó (Central Kingdom)
- Dōngfāng (Eastern Direction)
- Tiānxià (Under Heaven)

### Korean Names

**Burgs:**

- Hanseongsi (Han Fortress City)
- Baekdusan (White Head Mountain)
- Namgangdo (South River Province)
- Donghaeri (East Sea Village)

**States:**

- Hanguk (Korea)
- Joseon (Morning Calm)
- Goryeo (High Serenity)

## Special Considerations

### Romanization Systems

**Japanese:** Modified Hepburn

- し → shi (not si)
- ち → chi (not ti)
- つ → tsu (not tu)
- ん → n

**Chinese:** Simplified Pinyin

- No tone marks (mā → ma)
- ü → ü or v
- Standard consonants

**Korean:** Revised Romanization

- 어 → eo (not ŏ)
- 으 → eu
- 의 → ui
- No apostrophes

### Cultural Sensitivity

- Names should be **respectful** and **culturally appropriate**
- Avoid **offensive meanings** (check morpheme semantics)
- Use **authentic** phonological patterns
- Reference **real place names** for validation

### Writing Systems (Future)

While current output is romanized, future versions could support:

- **Japanese:** Hiragana/Katakana/Kanji
- **Chinese:** Simplified/Traditional characters
- **Korean:** Hangul syllable blocks

## Testing Strategy

### Phonological Tests

- [ ] Verify syllable structure constraints
- [ ] Check forbidden sequences
- [ ] Validate allophonic rules
- [ ] Test consonant/vowel clusters

### Linguistic Tests

- [ ] Compare with real place names
- [ ] Check pronounceability
- [ ] Verify morpheme usage
- [ ] Validate romanization

### Cultural Tests

- [ ] Review with native speakers (if possible)
- [ ] Check for offensive meanings
- [ ] Verify naming patterns
- [ ] Validate cultural authenticity

## Success Criteria

- [ ] 3 CJK language templates created
- [ ] All phonological rules implemented correctly
- [ ] Romanization systems working
- [ ] Names sound authentic to native speakers
- [ ] No offensive or nonsensical names
- [ ] Documentation complete
- [ ] All tests passing

## Benefits

1. **Cultural Diversity**: East Asian representation
2. **Global Appeal**: Useful for worldwide game development
3. **Linguistic Accuracy**: Real phonological patterns
4. **Educational Value**: Learn about CJK languages
5. **Fantasy Settings**: Asian-inspired worlds

## Future Enhancements

- **Vietnamese** (Austroasiatic, tonal, Latin script)
- **Thai** (Tai-Kadai, tonal, complex orthography)
- **Tagalog** (Austronesian)
- **Hindi/Sanskrit** (Indo-Aryan)
- **Classical Chinese** (Literary Chinese)
- **Old Japanese** (Man'yōgana)

## References

### Linguistic Resources

- **Japanese Phonology**: https://en.wikipedia.org/wiki/Japanese_phonology
- **Mandarin Phonology**: https://en.wikipedia.org/wiki/Standard_Chinese_phonology
- **Korean Phonology**: https://en.wikipedia.org/wiki/Korean_phonology
- **IPA Chart**: https://www.internationalphoneticassociation.org/content/ipa-chart

### Romanization Standards

- **Japanese Hepburn**: https://en.wikipedia.org/wiki/Hepburn_romanization
- **Chinese Pinyin**: https://en.wikipedia.org/wiki/Pinyin
- **Korean RR**: https://en.wikipedia.org/wiki/Revised_Romanization_of_Korean

### Place Name Research

- **Japanese Place Names**: https://www.japantimes.co.jp/culture/place-names/
- **Chinese Place Names**: http://www.chinatoday.com.cn/
- **Korean Place Names**: https://www.korea.net/

## Dependencies

- RFC 016: JSON Configuration System (required)
- Spec 014: Name Generation System (base)

## Related Work

- Unicode support for CJK characters (future)
- Font rendering for East Asian scripts (future)
- Tone generation for tonal languages (future)
