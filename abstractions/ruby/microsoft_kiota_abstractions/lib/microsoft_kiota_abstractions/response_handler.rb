require 'concurrent'

module MicrosoftKiotaAbstractions
  module ResponseHandler
    include Concurrent::Async

    def handle_response_async(response)
      raise NotImplementedError.new
    end
    
  end
end
