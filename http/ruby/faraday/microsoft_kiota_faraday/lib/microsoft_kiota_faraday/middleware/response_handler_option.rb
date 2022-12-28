# frozen_string_literal: true
require 'microsoft_kiota_abstractions'
module MicrosoftKiotaFaraday
    module Middleware
        class ResponseHandlerOption
            RESPONSE_HANDLER_KEY = "responseHandler"
            # a lambda that takes the native response type and returns a Fiber with a MicrosoftKiotaAbstractions::Parsable
            attr_accessor :async_callback
            def get_key()
                RESPONSE_HANDLER_KEY
            end
        end
    end
end
