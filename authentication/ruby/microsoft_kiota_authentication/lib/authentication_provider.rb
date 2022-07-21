module MicrosoftKiotaAuthentication
  module AuthenticationProvider

    def authenticate_request(request)
      raise NotImplementedError.new
    end
    
  end
end
