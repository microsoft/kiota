class AuthenticationProvider
    include MicrosoftKiotaAbstractions::AuthenticationProvider
    include Concurrent::Async
end