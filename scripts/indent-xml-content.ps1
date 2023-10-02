Function Format-XMLIndent {
    param
    (
        [xml]$Content
    )

    # String Writer and XML Writer objects to write XML to string
    $StringWriter = New-Object System.IO.StringWriter 
    $XmlWriter = New-Object System.XMl.XmlTextWriter $StringWriter 

    # Default = None, change Formatting to Indented
    $xmlWriter.Formatting = "indented" 

    # Gets or sets how many IndentChars to write for each level in 
    # the hierarchy when Formatting is set to Formatting.Indented
    $xmlWriter.Indentation = 2
    
    $Content.WriteContentTo($XmlWriter) 
    $XmlWriter.Flush();
    $StringWriter.Flush() 
    return $StringWriter.ToString()
}