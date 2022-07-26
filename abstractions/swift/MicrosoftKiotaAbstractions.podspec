Pod::Spec.new do |s|
    s.name        = "MicrosoftKiotaAbstractions"
    s.version     = "1.0.0"
    s.summary     = "MicrosoftKiotaAbstractions provides the base infrastructure for the Kiota-generated SDKs to function.
        It defines multiple concepts related to abstract HTTP requests, serialization, and authentication.
        These concepts can then be implemented independently without tying the SDKs to any specific implementation.
        Kiota also provides default implementations for these concepts."
    s.homepage    = "https://github.com/microsoft/kiota"
    s.license     = { :type => "MIT" }
    s.authors     = { "Microsoft" => "graphtooling+kiota@service.microsoft.com" }
  
    s.requires_arc = true
    s.swift_version = "5.0"
    s.osx.deployment_target = "10.9"
    s.ios.deployment_target = "9.0"
    s.watchos.deployment_target = "3.0"
    s.tvos.deployment_target = "9.0"
    s.source   = { :git => "https://github.com/microsoft/kiota.git", :tag => s.version }
    s.source_files = "Source/*.swift"
end