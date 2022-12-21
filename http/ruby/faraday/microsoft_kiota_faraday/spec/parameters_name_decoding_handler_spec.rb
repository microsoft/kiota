require 'faraday'

# frozen_string_literal: true
RSpec.describe MicrosoftKiotaFaraday do
    it "decodes encoded query parameters" do
        values = Hash.new
        values["?%24select=diplayName&api%2Dversion=2"] = "?$select=diplayName&api-version=2"
        values["?%24select=diplayName&api%7Eversion=2"] = "?$select=diplayName&api~version=2"
        values["?%24select=diplayName&api%2Eversion=2"] = "?$select=diplayName&api.version=2"
        values["/api-version/?%24select=diplayName&api%2Eversion=2"] = "/api-version/?$select=diplayName&api.version=2"
        values[""] = ""

        handler = MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingHandler.new()
        values.each do |key, value|
            request = Faraday::Request.create(:get) do |req|
                req.path = URI.parse("https://graph.microsoft.com/v1.0/users#{key}")
            end
            handler.call(request)
            expect(request.path.to_s).to eq("https://graph.microsoft.com/v1.0/users#{value}")
        end
    end

    it "doesn't decode when disabled on handler" do
        values = Hash.new
        values["?%24select=diplayName&api%2Dversion=2"] = "?%24select=diplayName&api%2Dversion=2"
        values["?%24select=diplayName&api%7Eversion=2"] = "?%24select=diplayName&api%7Eversion=2"
        values["?%24select=diplayName&api%2Eversion=2"] = "?%24select=diplayName&api%2Eversion=2"
        values["/api-version/?%24select=diplayName&api%2Eversion=2"] = "/api-version/?%24select=diplayName&api%2Eversion=2"
        values[""] = ""

        option = MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingOption.new(false)
        handler = MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingHandler.new(nil, option)
        values.each do |key, value|
            request = Faraday::Request.create(:get) do |req|
                req.path = URI.parse("https://graph.microsoft.com/v1.0/users#{key}")
                req.options = Faraday::RequestOptions.new
                req.options.context = { }
            end
            handler.call(request)
            expect(request.path.to_s).to eq("https://graph.microsoft.com/v1.0/users#{value}")
        end
    end

    it "doesn't decode when disabled on request" do
        values = Hash.new
        values["?%24select=diplayName&api%2Dversion=2"] = "?%24select=diplayName&api%2Dversion=2"
        values["?%24select=diplayName&api%7Eversion=2"] = "?%24select=diplayName&api%7Eversion=2"
        values["?%24select=diplayName&api%2Eversion=2"] = "?%24select=diplayName&api%2Eversion=2"
        values["/api-version/?%24select=diplayName&api%2Eversion=2"] = "/api-version/?%24select=diplayName&api%2Eversion=2"
        values[""] = ""

        option = MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingOption.new(false)
        handler = MicrosoftKiotaFaraday::Middleware::ParametersNameDecodingHandler.new()
        values.each do |key, value|
            request = Faraday::Request.create(:get) do |req|
                req.path = URI.parse("https://graph.microsoft.com/v1.0/users#{key}")
                req.options = Faraday::RequestOptions.new
                req.options.context = { option.get_key => option }
            end
            handler.call(request)
            expect(request.path.to_s).to eq("https://graph.microsoft.com/v1.0/users#{value}")
        end
    end
end
