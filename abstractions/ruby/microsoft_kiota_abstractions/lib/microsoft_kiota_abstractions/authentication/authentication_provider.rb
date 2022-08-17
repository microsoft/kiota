module MicrosoftKiotaAbstractions
  module AuthenticationProvider
    def authenticate_request(request, additional_properties = {})
      raise NotImplementedError.new
    end 
  end
end
