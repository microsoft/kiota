require_relative 'request_information'
require_relative 'response_handler'

module MicrosoftKiotaAbstractions
  module RequestAdapter
    include ResponseHandler

    def send_async(request_info, factory, response_handler)
      raise NotImplementedError.new
    end

    # TODO we're most likley missing something for enums and collections or at least at the implemenation level

    def get_serialization_writer_factory()
      raise NotImplementedError.new
    end

    def set_base_url(base_url)
      raise NotImplementedError.new
    end

    def get_base_url()
      raise NotImplementedError.new
    end

  end
end
