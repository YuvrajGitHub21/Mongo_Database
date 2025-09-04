using IDV_Templates_Mongo_API.Models;
using System.Collections.Generic;

namespace IDV_Templates_Mongo_API.Services
{
    public static class DefaultTemplateFactory
    {
        // Creates a blank template with the name and all configurable fields false
        // (FirstName/LastName/Email remain true per your requirement)
        public static Template CreateBlank(string name)
        {
            return new Template
            {
                nameOfTemplate = name,

                Personal_info = new PersonalInfo
                {
                    section_id = 1,
                    firstName = true,
                    LastName = true,
                    Email = true,
                    Added_fields = new AddedFields
                    {
                        dob = false,
                        Current_address = false,
                        permanent_address = false,
                        Gender = false
                    }
                },

                Doc_verification = new DocVerification
                {
                    section_id = 2,
                    user_uploads = new UserUploads
                    {
                        Allow_uploads = false,
                        allow_capture = false
                    },
                    Unreadable_docs = new UnreadableDocs
                    {
                        reject_immediately = false,
                        Allow_retries = false
                    },
                    // IMPORTANT: use CountryDocs to match your model
                    Countries_array = new List<CountryDocs>()
                },

                Biometric_verification = new BiometricVerification
                {
                    section_id = 3,
                    number_of_retries = new List<int>(),
                    liveness = new Liveness
                    {
                        try_again = false,
                        Block_further = false
                    },
                    biometric_data_retention = new BiometricDataRetention
                    {
                        duration = new List<string>()
                    }
                },

                Section_status = new SectionStatus
                {
                    persoanl_info = false,
                    doc_verification = false,
                    Biometric_verification = false
                },

                Template_status = false,
                sections_order = new List<string> { "Personal_info", "Doc_verification", "Biometric_verification" },
                current_step = 1,

                // meta fields are stamped by controller/service
                invitees = new List<Invitee>()
            };
        }
    }
}
