using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace KrishiSahayAI.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        private const string PrimaryModel = "gemini-3.8-flash";
        private const string FallbackModel = "gemini-3.7-flash";

        private const string InteractionsEndpoint =
            "https://generativelanguage.googleapis.com/v1beta/interactions";

        public GeminiService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _apiKey =
                Environment.GetEnvironmentVariable("GEMINI_API_KEY")
                ?? "";
        }


        // =========================================================
        // ASK KRISHISAHAY AI
        // =========================================================

        public async Task<string> AskAsync(
            string userQuestion,
            string farmContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception(
                    "GEMINI_API_KEY environment variable was not found.");
            }

            var systemInstruction = """
                You are KrishiSahay AI, a responsible agricultural
                decision-support assistant.

                Your purpose is to help farmers understand their farm,
                crop, soil, water and weather information.

                Use the supplied farm context carefully.

                IMPORTANT RULES:

                1. Never invent soil pH, nutrients, soil chemistry,
                   soil type or laboratory results.

                2. Never invent weather, rainfall, market prices,
                   government schemes, statistics or local conditions.

                3. Do not provide definitive medical-style diagnoses
                   for plants.

                4. Do not provide pesticide, fungicide, herbicide or
                   fertilizer dosage, concentration, mixing or spraying
                   schedules.

                5. Clearly distinguish:
                   - observed information
                   - possible explanations
                   - things that should be checked

                6. Recommend appropriate soil or water testing when
                   reliable measurements are missing.

                7. Do not guarantee crop yield or outcomes.

                8. If information is missing, say that it is missing.

                9. Keep answers practical and understandable for farmers.

                10. Stay focused on agriculture and the farmer's
                    supplied farm context.

                Structure responses using:

                SHORT ANSWER
                WHAT THIS MEANS
                WHAT TO CHECK
                NEXT STEPS
                IMPORTANT
                """;

            var prompt = $"""
                FARM CONTEXT
                ========================================

                {farmContext}

                ========================================
                FARMER QUESTION
                ========================================

                {userQuestion}

                ========================================

                Answer the farmer using the farm context above.
                Do not assume information that is not supplied.
                """;

            var input = new object[]
            {
                new
                {
                    type = "text",
                    text = prompt
                }
            };

            return await SendInteractionAsync(
                systemInstruction,
                input);
        }

        // =========================================================
        // GEMINI FARM BUDGET ALLOCATION
        // =========================================================

        public async Task<string> AllocateFarmBudgetAsync(
            string farmContext)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception(
                    "GEMINI_API_KEY environment variable was not found.");
            }

            var systemInstruction = """
        You are KrishiSahay AI, an agricultural farm-budget
        planning assistant.

        Your task is to allocate the farmer's TOTAL FARM BUDGET
        across ALL crops on the farm.

        IMPORTANT RULES:

        1. The supplied farm budget is the TOTAL budget for the
           entire farm, not the budget for one crop.

        2. Consider ALL crops together.

        3. Consider:
           - total farm area
           - crop area
           - crop type
           - location
           - soil information
           - water source
           - farming method
           - season
           - farmer's stated budget

        4. Crop allocations do NOT have to be strictly
           proportional to area. Different crops can have
           different expected input requirements.

        5. The sum of ALL crop allocations MUST EXACTLY
           equal the supplied total farm budget.

        6. Do not allocate the full farm budget independently
           to every crop.

        7. Do not invent market quotations or claim that the
           estimates are exact current market prices.

        8. These are planning estimates only.

        9. Return ONLY valid JSON.
        Do not use markdown.
        Do not use ```json fences.

        Return exactly this structure:

        {
          "totalFarmBudget": 430000,
          "allocations": [
            {
              "cropName": "Wheat",
              "area": 3,
              "areaUnit": "Acre",
              "allocatedAmount": 200000,
              "percentageOfFarmBudget": 46.51
            }
          ]
        }

        The allocations array MUST contain every crop supplied
        in the farm context.

        The sum of allocatedAmount MUST equal totalFarmBudget.
        """;

            var prompt = $"""
        FARM INFORMATION
        ========================================

        {farmContext}

        ========================================

        Allocate the TOTAL FARM BUDGET across all crops.

        Consider the crop areas and crop-specific requirements,
        while ensuring that the total of all allocations exactly
        equals the total farm budget.

        Return ONLY the requested JSON.
        """;

            var input = new object[]
            {
        new
        {
            type = "text",
            text = prompt
        }
            };

            return await SendInteractionAsync(
                systemInstruction,
                input);
        }
        // =========================================================
        // AI CROP DOCTOR
        // =========================================================

        public async Task<string> AnalyzeCropImageAsync(
            string observation,
            string farmContext,
            byte[] imageBytes,
            string mimeType)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception(
                    "GEMINI_API_KEY environment variable was not found.");
            }

            if (imageBytes == null || imageBytes.Length == 0)
            {
                throw new Exception(
                    "The crop image is empty.");
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                mimeType = "image/jpeg";
            }

            var allowedMimeTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedMimeTypes.Contains(
                    mimeType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new Exception(
                    $"Unsupported image MIME type: {mimeType}");
            }

            var base64Image =
                Convert.ToBase64String(imageBytes);


            var systemInstruction = """
                You are KrishiSahay AI Crop Doctor.

                You are an agricultural image-analysis assistant.

                Analyze the supplied crop image together with the
                farmer's observation and farm context.

                IMPORTANT SAFETY RULES:

                1. Do not claim a definitive disease diagnosis.

                2. Do not invent visual symptoms that cannot actually
                   be seen in the supplied image.

                3. Clearly separate:
                   - visible observations
                   - possible causes
                   - additional checks

                4. If the image quality is insufficient, say so.

                5. If the image does not appear to contain the stated
                   crop or plant, say so.

                6. Do not provide pesticide, fungicide, herbicide or
                   fertilizer dosage, concentration, mixing or spraying
                   schedules.

                7. Do not guarantee crop recovery or yield.

                8. Do not invent soil pH, nutrient levels, water quality,
                   weather conditions or laboratory results.

                9. Use the supplied farm context only.

                10. Recommend consultation with a local agricultural
                    expert or plant diagnostic service when the issue
                    is severe, rapidly spreading or uncertain.

                Return the response using these sections:

                VISUAL OBSERVATIONS
                POSSIBLE EXPLANATIONS
                WHAT TO CHECK NEXT
                FARM CONTEXT
                IMPORTANT NOTE

                LANGUAGE RULE
                --------------------------------------------------------
                Respond in the same language used by the farmer in
                the observation/question.

                This language rule applies to the COMPLETE response,
                including:
                - section headings
                - titles
                - bullet points
                - numbered lists
                - explanations
                - recommendations
                - warnings and notes

                Do not use English section headings when the farmer
                is writing in Gujarati, Hindi, Marathi, Bengali,
                Kannada, Malayalam, Punjabi, Tamil or Telugu.

                If the farmer's observation is written in a regional
                Indian language, respond entirely in that language.
                Keep only unavoidable technical, scientific, crop,
                pest or product names in their commonly used original
                form when necessary.

                If no farmer observation/question is provided, respond
                in English.

                Keep the language clear and practical for a farmer.
                """;


            var cropDoctorPrompt = $"""
                FARM INFORMATION
                ========================================

                {farmContext}

                ========================================
                FARMER'S OBSERVATION
                ========================================

                {(string.IsNullOrWhiteSpace(observation)
                    ? "No additional observation was provided."
                    : observation)}

                ========================================

                Analyze the supplied crop image.

                First describe only what can reasonably be observed
                from the image.

                Then explain possible causes without claiming certainty.

                Use the farm context to make the explanation relevant,
                but do not invent missing information.

                Recommend useful next checks.

                IMPORTANT LANGUAGE REQUIREMENT:
                The farmer's observation above may be written in a
                regional Indian language. Detect the language of the
                farmer's observation and write the COMPLETE analysis
                in that same language, including all headings and
                section names. If the observation is empty, use
                English.
                """;


            var input = new object[]
            {
                new
                {
                    type = "text",
                    text = cropDoctorPrompt
                },
                new
                {
                    type = "image",
                    data = base64Image,
                    mime_type = mimeType
                }
            };


            return await SendInteractionAsync(
                systemInstruction,
                input);
        }


        // =========================================================
        // GEMINI 3.5 TRANSCRIBE - AUTOMATIC LANGUAGE DETECTION
        // =========================================================

        public async Task<SpeechTranscriptionResult> TranscribeAudioAsync(
            byte[] audioBytes,
            string mimeType)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                throw new Exception(
                    "GEMINI_API_KEY environment variable was not found.");
            }

            if (audioBytes == null || audioBytes.Length == 0)
            {
                throw new Exception("The audio recording is empty.");
            }

            if (audioBytes.Length > 10 * 1024 * 1024)
            {
                throw new Exception("The audio recording is too large.");
            }

            if (string.IsNullOrWhiteSpace(mimeType))
            {
                mimeType = "audio/webm";
            }

            var allowedMimeTypes = new[]
            {
                "audio/webm",
                "audio/opus",
                "audio/ogg",
                "audio/wav",
                "audio/mp3",
                "audio/mpeg",
                "audio/mp4",
                "audio/m4a",
                "audio/aac",
                "audio/flac"
            };

            if (!allowedMimeTypes.Contains(
                    mimeType,
                    StringComparer.OrdinalIgnoreCase))
            {
                throw new Exception(
                    $"Unsupported audio MIME type: {mimeType}");
            }

            string? fileUri = null;

            try
            {
                fileUri = await UploadAudioFileAsync(
                    audioBytes,
                    mimeType);

                Exception? lastException = null;
                int[] delays = { 0, 2000, 5000 };

                for (int attempt = 0; attempt < delays.Length; attempt++)
                {
                    try
                    {
                        if (delays[attempt] > 0)
                        {
                            await Task.Delay(delays[attempt]);
                        }

                        var requestObject = new
                        {
                            model = "gemini-3.5-transcribe",
                            input = new object[]
                            {
                                new
                                {
                                    type = "audio",
                                    uri = fileUri,
                                    mime_type = mimeType
                                }
                            },
                            generation_config = new
                            {
                                transcription_config = new
                                {
                                    // Empty means automatic language detection.
                                    language_codes = Array.Empty<string>(),
                                    mode = "smart"
                                }
                            }
                        };

                        var json = JsonSerializer.Serialize(requestObject);

                        using var request = new HttpRequestMessage(
                            HttpMethod.Post,
                            InteractionsEndpoint);

                        request.Headers.TryAddWithoutValidation(
                            "x-goog-api-key",
                            _apiKey);

                        request.Content = new StringContent(
                            json,
                            Encoding.UTF8,
                            "application/json");

                        using var response =
                            await _httpClient.SendAsync(request);

                        var responseBody =
                            await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            var transcript =
                                ExtractTextFromInteraction(responseBody);

                            return new SpeechTranscriptionResult
                            {
                                Transcript = transcript,
                                LanguageCode = DetectLanguageFromTranscript(transcript)
                            };
                        }

                        var statusCode = (int)response.StatusCode;
                        var message = ExtractGeminiErrorMessage(responseBody);

                        var apiException = new GeminiApiException(
                            statusCode,
                            message,
                            responseBody);

                        lastException = apiException;

                        if (statusCode != 429 && statusCode < 500)
                        {
                            throw apiException;
                        }

                        if (attempt == delays.Length - 1)
                        {
                            break;
                        }
                    }
                    catch (GeminiApiException ex)
                    {
                        lastException = ex;

                        if (ex.StatusCode != 429 && ex.StatusCode < 500)
                        {
                            throw;
                        }

                        if (attempt == delays.Length - 1)
                        {
                            break;
                        }
                    }
                }

                throw new Exception(
                    "Speech transcription could not be completed after retrying. " +
                    $"Last error: {lastException?.Message}",
                    lastException);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(fileUri))
                {
                    await TryDeleteGeminiFileAsync(fileUri);
                }
            }
        }

        private async Task<string> UploadAudioFileAsync(
            byte[] audioBytes,
            string mimeType)
        {
            var startUrl =
                "https://generativelanguage.googleapis.com/upload/v1beta/files";

            using var startRequest = new HttpRequestMessage(
                HttpMethod.Post,
                startUrl);

            startRequest.Headers.TryAddWithoutValidation(
                "x-goog-api-key",
                _apiKey);

            startRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Protocol",
                "resumable");

            startRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Command",
                "start");

            startRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Header-Content-Length",
                audioBytes.Length.ToString());

            startRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Header-Content-Type",
                mimeType);

            var metadata = JsonSerializer.Serialize(
                new
                {
                    file = new
                    {
                        display_name = $"krishisahay-voice-{Guid.NewGuid():N}"
                    }
                });

            startRequest.Content = new StringContent(
                metadata,
                Encoding.UTF8,
                "application/json");

            using var startResponse =
                await _httpClient.SendAsync(startRequest);

            var startBody =
                await startResponse.Content.ReadAsStringAsync();

            if (!startResponse.IsSuccessStatusCode)
            {
                throw new GeminiApiException(
                    (int)startResponse.StatusCode,
                    ExtractGeminiErrorMessage(startBody),
                    startBody);
            }

            if (!startResponse.Headers.TryGetValues(
                    "X-Goog-Upload-URL",
                    out var uploadUrls))
            {
                throw new Exception(
                    "Gemini Files API did not return an upload URL.");
            }

            var uploadUrl = uploadUrls.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                throw new Exception(
                    "Gemini Files API returned an empty upload URL.");
            }

            using var uploadRequest = new HttpRequestMessage(
                HttpMethod.Post,
                uploadUrl);

            uploadRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Offset",
                "0");

            uploadRequest.Headers.TryAddWithoutValidation(
                "X-Goog-Upload-Command",
                "upload, finalize");

            var content = new ByteArrayContent(audioBytes);
            content.Headers.ContentType =
                new MediaTypeHeaderValue(mimeType);

            uploadRequest.Content = content;

            using var uploadResponse =
                await _httpClient.SendAsync(uploadRequest);

            var uploadBody =
                await uploadResponse.Content.ReadAsStringAsync();

            if (!uploadResponse.IsSuccessStatusCode)
            {
                throw new GeminiApiException(
                    (int)uploadResponse.StatusCode,
                    ExtractGeminiErrorMessage(uploadBody),
                    uploadBody);
            }

            using var uploadDocument =
                JsonDocument.Parse(uploadBody);

            if (!uploadDocument.RootElement.TryGetProperty(
                    "file",
                    out var fileElement)
                ||
                !fileElement.TryGetProperty(
                    "uri",
                    out var uriElement))
            {
                throw new Exception(
                    "Gemini Files API did not return a file URI.");
            }

            var fileUri = uriElement.GetString();

            if (string.IsNullOrWhiteSpace(fileUri))
            {
                throw new Exception(
                    "Gemini Files API returned an empty file URI.");
            }

            return fileUri;
        }

        private async Task TryDeleteGeminiFileAsync(
            string fileUri)
        {
            try
            {
                var fileName = fileUri;

                if (fileName.Contains('/'))
                {
                    fileName = fileName.Substring(
                        fileName.LastIndexOf('/') + 1);
                }

                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return;
                }

                var deleteUrl =
                    "https://generativelanguage.googleapis.com/v1beta/files/" +
                    Uri.EscapeDataString(fileName);

                using var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    deleteUrl);

                request.Headers.TryAddWithoutValidation(
                    "x-goog-api-key",
                    _apiKey);

                using var response =
                    await _httpClient.SendAsync(request);

                // Deletion is cleanup only. Never hide the transcription result
                // if the cleanup request fails.
            }
            catch
            {
                // Ignore cleanup failures.
            }
        }

        private static string DetectLanguageFromTranscript(
            string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return "en";
            }

            foreach (var ch in transcript)
            {
                var code = (int)ch;

                if (code >= 0x0A80 && code <= 0x0AFF)
                    return "gu";

                if (code >= 0x0980 && code <= 0x09FF)
                    return "bn";

                if (code >= 0x0A00 && code <= 0x0A7F)
                    return "pa";

                if (code >= 0x0B80 && code <= 0x0BFF)
                    return "ta";

                if (code >= 0x0C00 && code <= 0x0C7F)
                    return "te";

                if (code >= 0x0C80 && code <= 0x0CFF)
                    return "kn";

                if (code >= 0x0D00 && code <= 0x0D7F)
                    return "ml";

                if (code >= 0x0900 && code <= 0x097F)
                {
                    var lower = transcript.ToLowerInvariant();

                    var marathiHints = new[]
                    {
                        "आहे", "आणि", "काय", "तुम्ही", "माझे", "माझ्या",
                        "शेत", "पिकाला", "पिकाची", "कसे", "कशी", "करावे"
                    };

                    return marathiHints.Any(lower.Contains)
                        ? "mr"
                        : "hi";
                }
            }

            return "en";
        }


        // =========================================================
        // COMMON INTERACTIONS API REQUEST
        // =========================================================

        private async Task<string> SendInteractionAsync(
            string systemInstruction,
            object input)
        {
            Exception? lastException = null;

            // -----------------------------------------------------
            // GEMINI 3.8 FLASH
            // -----------------------------------------------------
            // Retry temporary 5xx/429 errors with exponential
            // backoff. This is especially important for Crop Doctor,
            // because image requests can receive temporary 503
            // service-unavailable responses during high demand.
            //
            // 400/401/403/etc. are NOT retried because they normally
            // indicate a request, authentication, or configuration
            // problem rather than temporary service availability.
            // -----------------------------------------------------

            int[] primaryDelays = { 0, 2000, 4000 };

            for (int attempt = 0; attempt < primaryDelays.Length; attempt++)
            {
                try
                {
                    if (primaryDelays[attempt] > 0)
                    {
                        await Task.Delay(primaryDelays[attempt]);
                    }

                    return await SendRequestAsync(
                        PrimaryModel,
                        systemInstruction,
                        input);
                }
                catch (GeminiApiException ex)
                {
                    lastException = ex;

                    // Do not retry client/authentication errors.
                    if (ex.StatusCode != 429 && ex.StatusCode < 500)
                    {
                        throw;
                    }

                    // 429/5xx are temporary/retryable errors.
                    if (attempt == primaryDelays.Length - 1)
                    {
                        break;
                    }
                }
            }


            // -----------------------------------------------------
            // GEMINI 3.7 FLASH FALLBACK
            // -----------------------------------------------------
            // Give the primary model some time before moving to the
            // fallback model. The fallback is also retried for
            // temporary 429/5xx responses.
            // -----------------------------------------------------

            int[] fallbackDelays = { 5000, 10000 };

            for (int attempt = 0; attempt < fallbackDelays.Length; attempt++)
            {
                try
                {
                    await Task.Delay(fallbackDelays[attempt]);

                    return await SendRequestAsync(
                        FallbackModel,
                        systemInstruction,
                        input);
                }
                catch (GeminiApiException ex)
                {
                    lastException = ex;

                    // Do not retry client/authentication errors.
                    if (ex.StatusCode != 429 && ex.StatusCode < 500)
                    {
                        throw;
                    }

                    if (attempt == fallbackDelays.Length - 1)
                    {
                        break;
                    }
                }
            }


            // -----------------------------------------------------
            // FINAL FALLBACK: GEMINI 3.6 FLASH
            // -----------------------------------------------------
            // Gemini 3.6 Flash is a currently supported stable model
            // and is used only after the primary and first fallback
            // have both returned temporary errors.
            // -----------------------------------------------------

            try
            {
                await Task.Delay(5000);

                return await SendRequestAsync(
                    "gemini-3.6-flash",
                    systemInstruction,
                    input);
            }
            catch (GeminiApiException ex)
            {
                lastException = ex;

                if (ex.StatusCode == 429)
                {
                    throw new Exception(
                        "KrishiSahay AI has reached the current Gemini API " +
                        "request limit. Please try again after the quota " +
                        "resets or use a Gemini API project with available quota.",
                        ex);
                }

                throw new Exception(
                    "Gemini request could not be completed after retrying " +
                    "the available models. " +
                    $"Last error: {lastException?.Message}",
                    ex);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    "Gemini request could not be completed after retrying " +
                    "the available models. " +
                    $"Last error: {lastException?.Message}",
                    ex);
            }
        }


        // =========================================================
        // SEND HTTP REQUEST
        // =========================================================

        private async Task<string> SendRequestAsync(
            string model,
            string systemInstruction,
            object input)
        {
            var requestObject = new
            {
                model = model,

                system_instruction = systemInstruction,

                input = input
            };


            var json =
                JsonSerializer.Serialize(
                    requestObject,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });


            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    InteractionsEndpoint);


            request.Headers.Add(
                "x-goog-api-key",
                _apiKey);


            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));


            request.Content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");


            using var response =
                await _httpClient.SendAsync(request);


            var responseBody =
                await response.Content.ReadAsStringAsync();


            // -----------------------------------------------------
            // ERROR HANDLING
            // -----------------------------------------------------

            if (!response.IsSuccessStatusCode)
            {
                var statusCode =
                    (int)response.StatusCode;

                string geminiMessage =
                    ExtractGeminiErrorMessage(
                        responseBody);


                throw new GeminiApiException(
                    statusCode,
                    geminiMessage,
                    responseBody);
            }


            // -----------------------------------------------------
            // PARSE RESPONSE
            // -----------------------------------------------------

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                throw new Exception(
                    "Gemini returned an empty response.");
            }


            return ExtractTextFromInteraction(
                responseBody);
        }


        // =========================================================
        // EXTRACT GEMINI ERROR
        // =========================================================

        private static string ExtractGeminiErrorMessage(
            string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return "Gemini returned an empty error response.";
            }

            try
            {
                using var document =
                    JsonDocument.Parse(responseBody);

                var root =
                    document.RootElement;


                if (root.TryGetProperty(
                        "error",
                        out var error))
                {
                    string code = "";

                    string message = "";


                    if (error.TryGetProperty(
                            "code",
                            out var codeElement))
                    {
                        code =
                            codeElement.ToString();
                    }


                    if (error.TryGetProperty(
                            "message",
                            out var messageElement))
                    {
                        message =
                            messageElement.GetString()
                            ?? "";
                    }


                    if (!string.IsNullOrWhiteSpace(message))
                    {
                        return string.IsNullOrWhiteSpace(code)
                            ? message
                            : $"{code}: {message}";
                    }
                }
            }
            catch
            {
                // Ignore JSON parsing failure.
            }


            return responseBody;
        }


        // =========================================================
        // EXTRACT TEXT FROM INTERACTION
        // =========================================================

        private static string ExtractTextFromInteraction(
            string responseBody)
        {
            using var document =
                JsonDocument.Parse(responseBody);

            var root =
                document.RootElement;


            // -----------------------------------------------------
            // output_text
            // -----------------------------------------------------

            if (root.TryGetProperty(
                    "output_text",
                    out var outputText))
            {
                var text =
                    outputText.GetString();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }


            // -----------------------------------------------------
            // outputs
            // -----------------------------------------------------

            if (root.TryGetProperty(
                    "outputs",
                    out var outputs)
                &&
                outputs.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (var output in outputs.EnumerateArray())
                {
                    if (output.TryGetProperty(
                            "text",
                            out var textElement))
                    {
                        var text =
                            textElement.GetString();

                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }
            }


            // -----------------------------------------------------
            // steps
            // -----------------------------------------------------

            if (root.TryGetProperty(
                    "steps",
                    out var steps)
                &&
                steps.ValueKind ==
                JsonValueKind.Array)
            {
                foreach (var step in steps.EnumerateArray())
                {
                    if (!step.TryGetProperty(
                            "type",
                            out var typeElement))
                    {
                        continue;
                    }

                    var type =
                        typeElement.GetString();


                    if (!string.Equals(
                            type,
                            "model_output",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }


                    if (!step.TryGetProperty(
                            "content",
                            out var content)
                        ||
                        content.ValueKind !=
                        JsonValueKind.Array)
                    {
                        continue;
                    }


                    foreach (
                        var contentBlock
                        in content.EnumerateArray())
                    {
                        if (contentBlock.TryGetProperty(
                                "type",
                                out var blockType)
                            &&
                            blockType.GetString()
                                == "text"
                            &&
                            contentBlock.TryGetProperty(
                                "text",
                                out var textElement))
                        {
                            var text =
                                textElement.GetString();

                            if (!string.IsNullOrWhiteSpace(text))
                            {
                                return text;
                            }
                        }
                    }
                }
            }


            throw new Exception(
                "Gemini returned a response, but no text output " +
                "could be found.");
        }


        // =========================================================
        // RETRY RULE
        // =========================================================

        private static bool IsRetryableException(
            Exception exception)
        {
            if (exception is GeminiApiException geminiException)
            {
                return geminiException.StatusCode == 429
                    || geminiException.StatusCode >= 500;
            }

            return false;
        }
    }


    // =============================================================
    // GEMINI API EXCEPTION
    // =============================================================

    public class GeminiApiException : Exception
    {
        public int StatusCode { get; }

        public string ResponseBody { get; }


        public GeminiApiException(
            int statusCode,
            string message,
            string responseBody)
            : base(
                $"Gemini request failed with status " +
                $"{statusCode}: {message}")
        {
            StatusCode = statusCode;

            ResponseBody = responseBody;
        }
    }
}