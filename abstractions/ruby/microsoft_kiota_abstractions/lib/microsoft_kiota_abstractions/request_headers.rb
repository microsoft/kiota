module MicrosoftKiotaAbstractions
    class RequestHeaders
        def initialize()
            @headers = Hash.new
        end
        def add(key, value)
            if key.nil? || key.empty? || value.nil? || value.empty? then
              raise ArgumentError, 'key and value cannot be nil or empty'
            end
            existing_value = @headers[key]
            if existing_value.nil? then
              if value.kind_of?(Array) then
                @headers[key] = value
              else
                @headers[key] = Array[value.to_s]
              end
            else
              if value.kind_of?(Array) then
                @headers[key] = existing_value | value
              else
                existing_value << value.to_s
              end
            end
        end
        def get(key)
            if key.nil? || key.empty? then
              raise ArgumentError, 'key cannot be nil or empty'
            end
            return @headers[key]
        end
        def remove(key)
            if key.nil? || key.empty? then
              raise ArgumentError, 'key cannot be nil or empty'
            end
            @headers.delete(key)
        end
        def clear()
            @headers.clear()
        end
        def get_all()
            return @headers
        end
    end
end