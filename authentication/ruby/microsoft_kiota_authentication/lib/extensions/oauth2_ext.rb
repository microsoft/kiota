# frozen_string_literal: true

require 'oauth2'

# Extension of Oauth2 Library to Include On Behalf Of Grant Type
module OAuth2
  module Strategy
    class OnBehalfOf < Base 
      def get_token(params, response_opts = {})
        @client.get_token(params, response_opts)
      end
    end
  end
end

module OAuth2
  class Client
    def on_behalf_of
      @on_behalf_of ||= OAuth2::Strategy::OnBehalfOf.new(self)
    end
  end
end