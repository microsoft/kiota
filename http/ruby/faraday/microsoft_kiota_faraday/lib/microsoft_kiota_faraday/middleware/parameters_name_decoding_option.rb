# frozen_string_literal: true
require 'microsoft_kiota_abstractions'
module MicrosoftKiotaFaraday
    module Middleware
        class ParametersNameDecodingOption
            include MicrosoftKiotaAbstractions::RequestOption
            attr_accessor :enabled, :characters_to_decode

            def initialize(enabled = true, characters_to_decode = ['$', '.', '-', '~'])
                @enabled = enabled
                @characters_to_decode = characters_to_decode
            end

            def get_key()
                "parametersNameDecoding"
            end
        end
    end
end