require_relative 'request_information'
require_relative 'response_handler'

module MicrosoftKiotaAbstractions
  module RequestAdapter
    include ResponseHandler

    def send_async(request_info, type, response_handler)
      raise NotImplementedError.new
    end

    def get_serialization_writer_factory()
      raise NotImplementedError.new
    end

  end
end
