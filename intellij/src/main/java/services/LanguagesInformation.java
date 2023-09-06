package services;

import java.util.Map;
import com.fasterxml.jackson.annotation.JsonAnySetter;
import java.util.HashMap;

public class LanguagesInformation {
    private final Map<String, LanguageInformation> languages = new HashMap<>();

    @JsonAnySetter
    public void addLanguage(String key, LanguageInformation language) {
        languages.put(key, language);
    }
    public Map<String, LanguageInformation> getLanguages() {
        return languages;
    }
}