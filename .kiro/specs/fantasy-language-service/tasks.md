# Implementation Plan

- [ ] 1. Set up project structure and core contracts
  - Create `PigeonPea.Language.Contracts` project in `dotnet/game-essential/core/src/`
  - Define `ILanguageService` interface with language management, translation, and name generation methods
  - Create phonology contracts (`IPhonologyEngine`, `PhonemeInventory`, `SyllableTemplate`, `PhonologyRules`)
  - Create lexicon contracts (`ILexiconManager`, `LexiconEntry`, `PartOfSpeech`)
  - Create grammar contracts (`IGrammarEngine`, `GrammarRules`, `WordOrder`, `MorphologyRule`)
  - Create sound change contracts (`ISoundChangeEngine`, `SoundChangeRule`)
  - Define data models (`LanguageDefinition`, `NameGenerationOptions`, `SentenceTemplate`)
  - _Requirements: 1.1, 1.2, 1.3, 2.1, 2.2, 3.1, 3.2, 7.1, 8.1_

- [ ] 2. Implement phonology engine
  - [ ] 2.1 Create `PigeonPea.Language.Core` project
    - Set up project in `dotnet/game-essential/core/src/`
    - Add reference to `PigeonPea.Language.Contracts`
    - Configure logging and dependency injection
    - _Requirements: 1.1_

  - [ ] 2.2 Implement phoneme inventory validation
    - Create `PhonologyEngine` class implementing `IPhonologyEngine`
    - Implement `ValidatePhonemeInventory` method to check for non-empty vowels and consonants
    - Add validation for duplicate phonemes
    - _Requirements: 1.1, 1.5_

  - [ ] 2.3 Implement syllable template validation
    - Implement `ValidateSyllableTemplate` method
    - Support patterns: V, CV, VC, CVC, CCV, CCVC, CVCC, CCVCC
    - Validate that phonemes in templates exist in inventory
    - _Requirements: 1.2, 1.5_

  - [ ] 2.4 Implement syllable generation
    - Implement `GenerateSyllable` method using syllable templates
    - Support weighted template selection
    - Handle consonant clusters (initial, medial, final)
    - Use provided Random instance for deterministic generation
    - _Requirements: 1.3, 4.1_

  - [ ] 2.5 Implement word validation
    - Implement `IsValidWord` method to check phonotactic constraints
    - Validate syllable structure
    - Validate consonant clusters
    - _Requirements: 4.1_

  - [ ]* 2.6 Write property test for phoneme inventory validation
    - **Property 1: Phoneme inventory validation accepts all valid inventories**
    - **Validates: Requirements 1.1**

  - [ ]* 2.7 Write property test for syllable template validation
    - **Property 2: Syllable template validation correctly identifies valid patterns**
    - **Validates: Requirements 1.2**

  - [ ]* 2.8 Write property test for generated syllables
    - **Property 15: Generated names follow phonotactic constraints**
    - **Validates: Requirements 4.1**

- [ ] 3. Implement lexicon manager
  - [ ] 3.1 Create lexicon storage system
    - Create `LexiconManager` class implementing `ILexiconManager`
    - Use `Dictionary<string, Dictionary<string, LexiconEntry>>` for language-keyed storage
    - Implement `AddEntry` method with validation
    - _Requirements: 2.1, 2.2_

  - [ ] 3.2 Implement lexicon lookup methods
    - Implement `LookupByMeaning` for forward lookup (English → Fantasy)
    - Implement `LookupByWord` for reverse lookup (Fantasy → English)
    - Support multiple meanings per word
    - Implement `GetAllEntries` for bulk retrieval
    - _Requirements: 2.1, 2.3, 2.4_

  - [ ] 3.3 Implement lexicon serialization
    - Implement `SaveLexiconAsync` to export to JSON format
    - Implement `LoadLexiconAsync` to import from JSON format
    - Use `System.Text.Json` for serialization
    - Handle file I/O errors gracefully
    - _Requirements: 2.5, 8.1, 8.3_

  - [ ]* 3.4 Write property test for lexicon round-trip
    - **Property 6: Lexicon entry round-trip preserves mappings**
    - **Property 10: Lexicon serialization round-trip**
    - **Validates: Requirements 2.1, 2.5, 8.1, 8.3**

  - [ ]* 3.5 Write property test for lexicon queries
    - **Property 8: Lexicon query returns all matching entries**
    - **Validates: Requirements 2.3**

- [ ] 4. Implement grammar engine
  - [ ] 4.1 Create grammar engine core
    - Create `GrammarEngine` class implementing `IGrammarEngine`
    - Implement `ValidateGrammar` method
    - _Requirements: 3.1, 3.5_

  - [ ] 4.2 Implement word order transformations
    - Implement `ApplyWordOrder` method
    - Support all six word orders: SVO, SOV, VSO, VOS, OVS, OSV
    - Handle subject, verb, object identification
    - _Requirements: 3.1, 3.4_

  - [ ] 4.3 Implement morphology application
    - Implement `ApplyMorphology` method
    - Support prefix, suffix, infix, circumfix patterns
    - Handle pluralization, case marking, verb conjugation
    - Use pattern replacement with `{root}` placeholder
    - _Requirements: 3.2, 3.4_

  - [ ] 4.4 Implement compound word formation
    - Implement `FormCompound` method
    - Support multiple root combination patterns
    - Handle optional connectors between roots
    - Support different compound types (Noun_Noun, Adjective_Noun, etc.)
    - _Requirements: 3.3_

  - [ ]* 4.5 Write property test for word order transformations
    - **Property 11: Word order transformation correctness**
    - **Validates: Requirements 3.1, 3.4**

  - [ ]* 4.6 Write property test for compound formation
    - **Property 13: Compound word formation follows rules**
    - **Validates: Requirements 3.3**

- [ ] 5. Implement name generator
  - [ ] 5.1 Create name generator core
    - Create `NameGenerator` class
    - Accept `LanguageDefinition` in constructor
    - Store reference to `PhonologyEngine`
    - _Requirements: 4.1_

  - [ ] 5.2 Implement basic name generation
    - Implement `GenerateName` method
    - Generate syllables based on min/max syllable count
    - Combine syllables into complete names
    - Support seeded Random for determinism
    - _Requirements: 4.1, 4.2, 4.5_

  - [ ] 5.3 Implement morphological name generation
    - Add support for root + affix combinations
    - Use language's morphology rules
    - Support different name types (Personal, Place, Item, Clan, Title)
    - _Requirements: 4.3_

  - [ ] 5.4 Implement batch name generation
    - Implement `GenerateNames` method for multiple names
    - Ensure diversity in output
    - Maintain phonological consistency
    - _Requirements: 4.4_

  - [ ]* 5.5 Write property test for name length constraints
    - **Property 16: Name length respects syllable constraints**
    - **Validates: Requirements 4.2**

  - [ ]* 5.6 Write property test for deterministic generation
    - **Property 19: Seeded name generation is deterministic**
    - **Validates: Requirements 4.5**

  - [ ]* 5.7 Write property test for name diversity
    - **Property 18: Name generation produces diverse yet consistent output**
    - **Validates: Requirements 4.4**

- [ ] 6. Implement translation engine
  - [ ] 6.1 Create translation engine core
    - Create `TranslationEngine` class
    - Accept `LexiconManager` and `GrammarEngine` dependencies
    - _Requirements: 5.1, 6.1_

  - [ ] 6.2 Implement English tokenization
    - Implement tokenization method for English text
    - Split on whitespace and punctuation
    - Preserve punctuation as separate tokens
    - _Requirements: 5.1_

  - [ ] 6.3 Implement fantasy language tokenization
    - Implement morphology-aware tokenization
    - Split based on language's morphological boundaries
    - Handle affixes correctly
    - _Requirements: 6.1_

  - [ ] 6.4 Implement English to fantasy translation
    - Implement `TranslateToFantasy` method
    - Tokenize English input
    - Look up each token in lexicon
    - Apply target language grammar rules
    - Handle unknown words (generate or mark as untranslatable)
    - _Requirements: 5.1, 5.2, 5.3, 5.4_

  - [ ] 6.5 Implement fantasy to English translation
    - Implement `TranslateToEnglish` method
    - Tokenize fantasy language input
    - Perform reverse lexicon lookup
    - Apply reverse grammar transformation to SVO
    - Preserve unknown words
    - _Requirements: 6.1, 6.2, 6.3, 6.4_

  - [ ]* 6.6 Write property test for tokenization
    - **Property 20: English tokenization correctness**
    - **Property 24: Fantasy language tokenization respects morphology**
    - **Validates: Requirements 5.1, 6.1**

  - [ ]* 6.7 Write property test for translation round-trip
    - Test English → Fantasy → English preserves meaning
    - **Property 22: Grammar transformation applies word order correctly**
    - **Property 26: Reverse grammar transformation to English SVO**
    - **Validates: Requirements 5.3, 6.3**

- [ ] 7. Implement sound change engine
  - [ ] 7.1 Create sound change engine core
    - Create `SoundChangeEngine` class implementing `ISoundChangeEngine`
    - _Requirements: 7.1_

  - [ ] 7.2 Implement sound change rule parsing
    - Parse sound change rule patterns
    - Support context specifications (e.g., "_a" for "before 'a'")
    - Validate rule structure
    - _Requirements: 7.1_

  - [ ] 7.3 Implement single sound change application
    - Implement `ApplySoundChange` method
    - Apply source → target phoneme transformation
    - Check context conditions before applying
    - Handle phoneme mergers
    - _Requirements: 7.2, 7.3_

  - [ ] 7.4 Implement bulk sound change application
    - Implement `ApplySoundChanges` method
    - Apply rules in specified order
    - Transform multiple words efficiently
    - _Requirements: 7.2_

  - [ ] 7.5 Implement language derivation
    - Implement `DeriveLanguage` method
    - Apply all sound changes to parent language's lexicon
    - Create new `LanguageDefinition` for daughter language
    - Maintain phonological validity
    - _Requirements: 7.4, 8.4_

  - [ ]* 7.6 Write property test for sound change order
    - **Property 29: Sound change application order correctness**
    - **Validates: Requirements 7.2**

  - [ ]* 7.7 Write property test for contextual application
    - **Property 30: Contextual sound change application**
    - **Validates: Requirements 7.3**

- [ ] 8. Implement language definition repository
  - [ ] 8.1 Create repository core
    - Create `LanguageDefinitionRepository` class
    - Support YAML configuration file parsing using YamlDotNet
    - _Requirements: 8.1_

  - [ ] 8.2 Implement configuration loading
    - Implement `LoadLanguageAsync` method
    - Parse YAML configuration files
    - Validate configuration structure
    - Report validation errors with line numbers
    - _Requirements: 8.1, 8.2_

  - [ ] 8.3 Implement configuration saving
    - Implement `SaveLanguageAsync` method
    - Serialize `LanguageDefinition` to YAML
    - Include all components (phonology, grammar, lexicon path)
    - _Requirements: 8.3_

  - [ ] 8.4 Implement language inheritance
    - Support `parent_language_id` field in configuration
    - Load parent language first
    - Apply sound changes to derive child language
    - _Requirements: 8.4_

  - [ ] 8.5 Implement hot-reload support
    - Add file system watcher for configuration files
    - Reload language definitions when files change
    - Notify subscribers of reload events
    - _Requirements: 8.5_

  - [ ]* 8.6 Write property test for configuration round-trip
    - **Property 10: Lexicon serialization round-trip** (already covered in 3.4)
    - **Property 33: Language inheritance and derivation**
    - **Validates: Requirements 8.1, 8.3, 8.4**

- [ ] 9. Implement language service facade
  - [ ] 9.1 Create language service implementation
    - Create `LanguageService` class implementing `ILanguageService`
    - Coordinate all subsystems (phonology, lexicon, grammar, translation, name generation)
    - Manage loaded languages in memory
    - _Requirements: 1.4, 9.1_

  - [ ] 9.2 Implement language management methods
    - Implement `LoadLanguageAsync` to load from configuration
    - Implement `UnloadLanguageAsync` to remove from memory
    - Implement `GetLoadedLanguages` to list active languages
    - Maintain language isolation
    - _Requirements: 1.4, 9.1_

  - [ ] 9.3 Implement translation facade methods
    - Implement `TranslateAsync` method
    - Route to `TranslationEngine`
    - Handle language not loaded errors
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5_

  - [ ] 9.4 Implement name generation facade methods
    - Implement `GenerateName` method
    - Implement `GenerateNames` method for batch generation
    - Route to `NameGenerator`
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ] 9.5 Implement text generation facade methods
    - Implement `GenerateSentence` method using templates
    - Implement `GenerateParagraph` method for multi-sentence text
    - Use grammar engine and lexicon
    - _Requirements: 10.1, 10.2, 10.3, 10.5_

  - [ ]* 9.6 Write property test for language isolation
    - **Property 4: Language isolation maintains independence**
    - **Validates: Requirements 1.4**

- [ ] 10. Implement plugin integration
  - [ ] 10.1 Create plugin discovery mechanism
    - Implement plugin discovery in configured directories
    - Scan for assemblies implementing `ILanguageService`
    - _Requirements: 9.1_

  - [ ] 10.2 Implement plugin validation
    - Validate plugins implement required contracts
    - Check for required methods and properties
    - _Requirements: 9.2_

  - [ ] 10.3 Implement plugin registration
    - Register all valid plugins in service registry
    - Support multiple language plugins
    - _Requirements: 9.3_

  - [ ] 10.4 Implement service resolution
    - Resolve language service by language identifier
    - Route requests to appropriate plugin
    - _Requirements: 9.4_

  - [ ] 10.5 Implement error handling for plugin loading
    - Log errors when plugins fail to load
    - Continue loading other plugins
    - Provide detailed error messages
    - _Requirements: 9.5_

  - [ ]* 10.6 Write property test for plugin discovery
    - **Property 34: Plugin discovery completeness**
    - **Validates: Requirements 9.1**

  - [ ]* 10.7 Write property test for plugin resilience
    - **Property 38: Plugin load failure resilience**
    - **Validates: Requirements 9.5**

- [ ] 11. Create sample language definitions
  - [ ] 11.1 Create High Elvish language
    - Create `configs/high-elvish.yaml` configuration
    - Define phonology (soft consonants, vowel-rich)
    - Define grammar (SVO word order)
    - Create sample lexicon with 100+ words
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1_

  - [ ] 11.2 Create Dwarvish language
    - Create `configs/dwarvish.yaml` configuration
    - Define phonology (harsh consonants, guttural)
    - Define grammar (VSO word order, triconsonantal roots)
    - Create sample lexicon with 100+ words
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1_

  - [ ] 11.3 Create Draconic language
    - Create `configs/draconic.yaml` configuration
    - Define phonology (booming, ancient sounds)
    - Define grammar (verb-first, compound-heavy)
    - Create sample lexicon with 100+ words
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1_

  - [ ] 11.4 Create Goblin language
    - Create `configs/goblin.yaml` configuration
    - Define phonology (choppy, harsh plosives)
    - Define grammar (SOV word order, simple)
    - Create sample lexicon with 50+ words
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1_

  - [ ] 11.5 Create language derivation example
    - Create Ancient Elvish as parent language
    - Derive High Elvish with sound changes
    - Demonstrate language evolution
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 8.4_

- [ ] 12. Checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 13. Create integration tests and examples
  - [ ]* 13.1 Write integration test for full translation pipeline
    - Load language, translate English to fantasy, translate back
    - Verify meaning preservation
    - _Requirements: 5.1, 5.2, 5.3, 5.4, 5.5, 6.1, 6.2, 6.3, 6.4, 6.5_

  - [ ]* 13.2 Write integration test for name generation pipeline
    - Load language, generate names, validate phonotactics
    - _Requirements: 4.1, 4.2, 4.3, 4.4, 4.5_

  - [ ]* 13.3 Write integration test for language derivation
    - Load parent language, apply sound changes, verify daughter language
    - _Requirements: 7.1, 7.2, 7.3, 7.4, 8.4_

  - [ ]* 13.4 Write integration test for plugin system
    - Discover plugins, load languages, perform operations
    - _Requirements: 9.1, 9.2, 9.3, 9.4, 9.5_

  - [ ] 13.5 Create example console application
    - Create simple CLI for testing language service
    - Support commands: load, translate, generate-name, list-languages
    - Demonstrate all major features
    - _Requirements: All_

- [ ] 14. Create documentation
  - [ ] 14.1 Write API documentation
    - Document all public interfaces
    - Include code examples for common scenarios
    - Document configuration file format
    - _Requirements: All_

  - [ ] 14.2 Write language creation guide
    - Tutorial for creating new fantasy languages
    - Explain phonology, grammar, lexicon design
    - Provide templates and examples
    - _Requirements: 1.1, 1.2, 1.3, 2.1, 3.1, 8.1_

  - [ ] 14.3 Write plugin development guide
    - Tutorial for creating language plugins
    - Explain plugin contracts and registration
    - Provide plugin template
    - _Requirements: 9.1, 9.2, 9.3_

  - [ ] 14.4 Create sample language showcase
    - Generate sample names for each language
    - Show translation examples
    - Demonstrate language evolution
    - _Requirements: 4.1, 5.1, 7.4_

- [ ] 15. Final checkpoint - Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.
