package toolWindow;

import com.intellij.openapi.ui.DialogWrapper;
import com.intellij.ui.components.JBList;
import com.intellij.ui.components.JBScrollPane;
import services.KiotaSearchResultItem;

import javax.swing.*;
import java.awt.*;
import java.util.Map;
import java.util.Set;

/**
 * this class creates the dialog window for searchResult
 */
public class SearchResultsDialog extends DialogWrapper {
    private final Map<String, KiotaSearchResultItem> resultsMap;
    private String selectedValue = null;

    public SearchResultsDialog(Map<String, KiotaSearchResultItem> resultsMap) {
        super(true);
        this.resultsMap = resultsMap;

        setTitle("Search Results");
        setOKButtonText("Select");
        init();
    }
    protected JComponent createCenterPanel() {
        JPanel panel = new JPanel(new BorderLayout());

        if (!resultsMap.isEmpty()) {
            Set<String> keys = resultsMap.keySet();

            DefaultListModel<String> listModel = new DefaultListModel<>();
            for (String key : keys) {
                KiotaSearchResultItem item = resultsMap.get(key);
                String displayValue = key + " - " + item.getDescription();
                listModel.addElement(displayValue);
            }

            JBList<String> list = new JBList<>(listModel);
            list.addListSelectionListener(e -> {
                int selectedIndex = list.getSelectedIndex();
                if (selectedIndex >= 0 && selectedIndex < keys.size()) {
                    String selectedItem = keys.toArray(new String[0])[selectedIndex];
                    selectedValue = resultsMap.get(selectedItem).getDescriptionUrl();
                }
            });
            panel.add(new JBScrollPane(list), BorderLayout.CENTER);
            return panel;
        } else {
            return new JLabel("No search results found.");
        }
    }
    public String getSelectedValue() {
        return selectedValue;
    }
}