import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '@/components/ui/accordion.tsx';

const QUESTIONS = [
  {
    id: 'what-is-jellio',
    question: `What is Jellio++?`,
    answer: `Jellio++ is a plugin that connects Jellyfin with Stremio, enabling you to stream your Jellyfin media library directly within the Stremio interface. It fetches metadata and provides seamless playback across your devices.`,
  },
  {
    id: 'what-jellio-gives-stremio-users',
    question: `What does Jellio++ provide for Stremio users?`,
    answer: `It provides a way to stream your local media library directly in Stremio, letting you have all your media in one place.`,
  },
  {
    id: 'what-jellio-gives-jellyfin-users',
    question: `What does Jellio++ provide for Jellyfin users?`,
    answer: `It lets you use any Stremio client to play your Jellyfin media, including unusual clients like Meta Quest VR or various TV OSes.`,
  },
  {
    id: 'is-jellio-secure',
    question: `Is Jellio++ secure?`,
    answer: `Yes, Jellio++ is secure. It is installed as a Jellyfin addon and all communication happens directly between your Jellyfin server and the Stremio client without any external communications. The source code is open source, allowing you to review it. It uses Jellyfin's access tokens for authentication, so your Jellyfin credentials are not exposed. You can revoke access at any time from your Jellyfin account settings.`,
  },
  {
    id: 'how-revoke-access',
    question: `How do I revoke access?`,
    answer: `You can revoke Jellio++ access tokens in your Jellyfin dashboard under Settings → Devices. Simply delete the device named “Jellio++” to stop future access.`,
  },

  {
    id: 'where-find-support',
    question: `Where can I find support?`,
    answer: `You can find support on our GitHub repository by opening issues, join our Discord community, or email me at support@jellio.stream. Links are available in the top navigation bar.`,
  },
  {
    id: 'is-jellio-free',
    question: `Is Jellio++ free and open source?`,
    answer: `Yes, Jellio++ is completely free and open source under. Contributions are welcome on the GitHub repository.`,
  },
];

const FAQ = () => {
  return (
    <div className="mt-5 mb-5 border rounded-lg p-6">
      <h2 className="text-md font-semibold">Frequently Asked Questions</h2>
      <Accordion type="multiple" className="mt-4">
        {QUESTIONS.map((item) => (
          <AccordionItem value={item.id} key={item.id}>
            <AccordionTrigger>{item.question}</AccordionTrigger>
            <AccordionContent>{item.answer}</AccordionContent>
          </AccordionItem>
        ))}
      </Accordion>
    </div>
  );
};

export default FAQ;
