require 'faraday'
require_relative 'parameters_name_decoding_option'
module MicrosoftKiotaFaraday
    module Middleware
        class ParametersNameDecodingHandler < Faraday::Middleware
            @@default_option = ParametersNameDecodingOption.new
            def call(request_env)
                request_option = request_env[:request][:context][@@default_option.get_key] unless request_env[:request].nil? || request_env[:request][:context].nil?
                request_option = @@default_option if request_option.nil?
                unless request_env[:url].nil? || !request_option.enabled || request_option.characters_to_decode.nil? || request_option.characters_to_decode.empty? then
                    request_url = request_env[:url].to_s
                    request_option.characters_to_decode.each do |character|
                        request_url = request_url.gsub(get_regex_for_character(character), character)
                    end
                    request_env[:url] = URI.parse(request_url)
                end
                @app.call(request_env) unless @app.nil?
            end

            def get_regex_for_character(character)
                @regex_cache ||= Hash.new
                if @regex_cache[character].nil? then
                    @regex_cache[character] = Regexp.new("%#{character.ord.to_s(16)}", true)
                end
                return @regex_cache[character]
            end
        end
    end
end