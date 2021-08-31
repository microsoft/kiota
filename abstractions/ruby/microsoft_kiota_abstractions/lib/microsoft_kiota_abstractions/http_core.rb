require_relative 'request_information'
require_relative 'response_handler'

module MicrosoftKiotaAbstractions
  module HttpCore
    include ResponseHandler

    def sendAsync(request_info, type, response_handler)
      raise NotImplementedError.new
    end

  end
end
