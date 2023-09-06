package toolWindow;

import com.intellij.openapi.ui.DialogWrapper;
import com.intellij.ui.components.JBScrollPane;
import services.KiotaGenerationLanguage;
import services.LanguageDependency;
import services.LanguageInformation;
import javax.swing.*;
import javax.swing.text.SimpleAttributeSet;
import javax.swing.text.StyleConstants;
import javax.swing.text.StyledDocument;
import java.awt.*;
import java.util.List;
import java.util.Map;
public class LanguageInfoPopup extends DialogWrapper {
    private Map<String, LanguageInformation> languagesmap;
    private final KiotaGenerationLanguage language;

    public LanguageInfoPopup(Map<String, LanguageInformation> languageInfo, KiotaGenerationLanguage language) {
        super(true);
        this.languagesmap = languageInfo;
        this.language = language;

        setTitle("Language Information");
        init();
    }

    @Override
    protected JComponent createCenterPanel() {
        JPanel panel = new JPanel(new BorderLayout());

        JTextArea infoTextArea = new JTextArea();
        infoTextArea.setEditable(false);

        // Generate information text from the provided LanguageInformation object
        String infoText = generateLanguageInfoText();

        infoTextArea.setText(infoText);

        JBScrollPane scrollPane = new JBScrollPane(infoTextArea);
        panel.add(scrollPane, BorderLayout.CENTER);

        return panel;
    }

    private String generateLanguageInfoText() {
        LanguageInformation selectedLanguage = languagesmap.get(language.toString());
        StringBuilder builder = new StringBuilder();
        if (selectedLanguage != null) {
            JTextPane textPane = new JTextPane();
            builder.append("Language: ").append(language.toString()).append("\n\n");

            // Append Dependencies
            builder.append("Dependencies:\n");
            List<LanguageDependency> dependencies = selectedLanguage.Dependencies;
            if (dependencies != null) {
                for (LanguageDependency dependency : dependencies) {
                    builder.append(dependency.Name).append(" (").append(dependency.Version).append(")\n");
                }
            }
            // Append Installation Commands
            builder.append("\nInstallation Commands:\n");
            if (dependencies != null) {
                for (LanguageDependency dependency : dependencies) {
                    String formattedInstallationCommand = selectedLanguage.DependencyInstallCommand
                            .replace("{0}", dependency.Name)
                            .replace("{1}", dependency.Version);
                    builder.append(formattedInstallationCommand).append("\n");
                }
            }

            textPane.setText(builder.toString());
            textPane.setEditable(false);

            return textPane.getText();
        } else {
            builder.append("Language not found.");
            return builder.toString();
        }
    }
}