module MicrosoftKiotaAbstractions
    module AdditionalDataHolder
      def additional_data 
        @additional_data ||= Hash.new
      end
    end
end
